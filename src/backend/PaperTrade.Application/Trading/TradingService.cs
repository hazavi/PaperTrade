using Microsoft.Extensions.Logging;
using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Application.Markets;
using PaperTrade.Domain.Instruments;
using PaperTrade.Domain.Orders;
using PaperTrade.Domain.Portfolios;
using PaperTrade.Domain.Positions;
using PaperTrade.Domain.Trades;

namespace PaperTrade.Application.Trading;

public sealed class TradingService(
    IPortfolioRepository portfolioRepository,
    ITradingRepository tradingRepository,
    IUnitOfWork unitOfWork,
    IMarketDataService marketDataService,
    IInstrumentCatalog instrumentCatalog,
    IRiskAnalyticsRepository riskAnalytics,
    TradingSimulationOptions options,
    ILogger<TradingService> logger) : ITradingService
{
    private readonly TradingSimulationOptions _options = options;

    public async Task<OrderExecutionResult> PlaceOrderAsync(Guid userId,
        CreateOrderRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrderSide>(request.Side, true, out var side) ||
            !Enum.TryParse<OrderType>(request.Type, true, out var type) ||
            !ValidSimulationOptions())
            return Invalid();

        var symbol = request.Symbol.Trim().ToUpperInvariant();
        var instrument = await instrumentCatalog.GetOrCreateAsync(symbol, cancellationToken);
        if (!instrument.IsTradable || request.Quantity < instrument.MinimumOrderSize ||
            decimal.Round(request.Quantity, instrument.QuantityPrecision) != request.Quantity ||
            request.Quantity % instrument.MinimumOrderSize != 0)
            return Invalid();

        var quote = await marketDataService.GetQuoteAsync(symbol, cancellationToken);
        if (quote is null) return new(OrderExecutionStatus.QuoteNotFound, null, null, null);
        if (!ValidPrices(request, type, side, quote, instrument)) return Invalid();

        var now = DateTimeOffset.UtcNow;
        var requestedPrice = type is OrderType.Limit or OrderType.Stop
            ? request.Price!.Value : quote.CurrentPrice;

        return await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var portfolio = await portfolioRepository.GetByUserIdAsync(userId, token);
            if (portfolio is null)
                return new OrderExecutionResult(OrderExecutionStatus.PortfolioNotFound, null, null, null);

            var position = await tradingRepository.GetPositionAsync(portfolio.Id, instrument.Id, token);
            if (side == OrderSide.Sell && (position is null || position.Quantity < request.Quantity))
                return new OrderExecutionResult(OrderExecutionStatus.InsufficientQuantity, null,
                    portfolio.CashBalance, position?.Quantity ?? 0);

            var order = new Order(Guid.NewGuid(), portfolio.Id, symbol, side, type,
                request.Quantity, requestedPrice, now, instrument.Id,
                expiresAt: request.ExpiresAt);
            if (type is OrderType.Limit or OrderType.Stop && !ShouldFill(order, quote))
            {
                tradingRepository.AddOrder(order);
                return new OrderExecutionResult(OrderExecutionStatus.Pending,
                    MapOrder(order, instrument), portfolio.CashBalance, position?.Quantity ?? 0);
            }

            if (side == OrderSide.Buy && !await WithinRiskLimitsAsync(portfolio, order,
                instrument, quote, token))
                return new OrderExecutionResult(OrderExecutionStatus.RiskLimitExceeded, null,
                    portfolio.CashBalance, position?.Quantity ?? 0);
            var result = FillOrder(order, portfolio, position, instrument, quote, now);
            if (result.Status != OrderExecutionStatus.Filled) return result;
            tradingRepository.AddOrder(order);
            if (type == OrderType.Bracket)
                AddBracketChildren(order, request, instrument, now);
            return result;
        }, cancellationToken);
    }

    public async Task<bool> CancelOrderAsync(Guid userId, Guid orderId, CancellationToken cancellationToken) =>
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var portfolio = await portfolioRepository.GetByUserIdAsync(userId, token);
            var order = await tradingRepository.GetOrderAsync(orderId, token);
            if (portfolio is null || order is null || order.PortfolioId != portfolio.Id ||
                order.Status is not (OrderStatus.Pending or OrderStatus.PartiallyFilled)) return false;
            order.Cancel(DateTimeOffset.UtcNow);
            return true;
        }, cancellationToken);

    public async Task ProcessPendingOrdersAsync(CancellationToken cancellationToken)
    {
        var openOrders = await tradingRepository.GetOpenOrdersAsync(cancellationToken);
        foreach (var snapshot in openOrders)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                var quote = snapshot.ExpiresAt <= now ? null :
                    await marketDataService.GetQuoteAsync(snapshot.Symbol, cancellationToken);
                if (quote is null && snapshot.ExpiresAt > now) continue;

                await unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var order = await tradingRepository.GetOrderAsync(snapshot.Id, token);
                    if (order is null || order.Status is not (OrderStatus.Pending or OrderStatus.PartiallyFilled))
                        return false;
                    if (order.ExpiresAt <= now)
                    {
                        order.Expire(now);
                        return true;
                    }
                    if (quote is null || !ShouldFill(order, quote)) return false;

                    var portfolio = await portfolioRepository.GetByIdAsync(order.PortfolioId, token);
                    if (portfolio is null) return false;
                    var position = await tradingRepository.GetPositionAsync(
                        portfolio.Id, order.InstrumentId, token);
                    var permitted = order.Side != OrderSide.Buy || await WithinRiskLimitsAsync(
                        portfolio, order, order.Instrument, quote, token);
                    var result = permitted
                        ? FillOrder(order, portfolio, position, order.Instrument, quote, now)
                        : new OrderExecutionResult(OrderExecutionStatus.RiskLimitExceeded, null,
                            portfolio.CashBalance, position?.Quantity ?? 0);
                    if (result.Status != OrderExecutionStatus.Filled)
                    {
                        order.Reject();
                        logger.LogInformation("Pending order {OrderId} rejected at trigger: {Reason}",
                            order.Id, result.Status);
                    }

                    if (order.ParentOrderId is Guid parentId)
                    {
                        var siblings = await tradingRepository.GetChildOrdersAsync(parentId, token);
                        foreach (var sibling in siblings.Where(item => item.Id != order.Id &&
                            item.Status is OrderStatus.Pending or OrderStatus.PartiallyFilled))
                            sibling.Cancel(now);
                    }
                    return true;
                }, cancellationToken);
            }
            catch (MarketDataUnavailableException exception)
            {
                logger.LogWarning(exception, "Cannot evaluate pending order {OrderId}", snapshot.Id);
            }
        }
    }

    public async Task<IReadOnlyList<OrderDto>> GetOrdersAsync(Guid userId,
        CancellationToken cancellationToken)
    {
        var portfolio = await portfolioRepository.GetByUserIdAsync(userId, cancellationToken);
        if (portfolio is null) return [];
        var orders = await tradingRepository.GetOrdersAsync(portfolio.Id, cancellationToken);
        return orders.Select(order => MapOrder(order)).ToArray();
    }

    public async Task<IReadOnlyList<ExecutionDto>> GetExecutionsAsync(Guid userId,
        CancellationToken cancellationToken)
    {
        var portfolio = await portfolioRepository.GetByUserIdAsync(userId, cancellationToken);
        if (portfolio is null) return [];
        var trades = await tradingRepository.GetExecutionsAsync(portfolio.Id, cancellationToken);
        return trades.Select(trade => new ExecutionDto(trade.Id, trade.OrderId, trade.Symbol,
            trade.Side.ToString().ToLowerInvariant(), trade.Quantity, trade.QuotePrice,
            trade.Price, trade.TotalValue, trade.Fee, trade.RealizedPnl, trade.ExecutedAt)).ToArray();
    }

    private OrderExecutionResult FillOrder(Order order, Portfolio portfolio, Position? position,
        Instrument instrument, MarketQuote quote, DateTimeOffset now)
    {
        var price = ExecutionPrice(order, quote, instrument);
        var quantity = order.RemainingQuantity;
        var gross = RoundMoney(AccountCurrency.NotionalUsd(instrument, quantity, price));
        var fee = order.Side == OrderSide.Buy
            ? RoundMoney(gross * _options.FeeBps / 10_000m) : 0;
        decimal realizedPnl;

        if (order.Side == OrderSide.Buy)
        {
            if (gross + fee > portfolio.CashBalance)
                return new(OrderExecutionStatus.InsufficientFunds, null, portfolio.CashBalance,
                    position?.Quantity ?? 0);
            portfolio.Debit(gross + fee);
            portfolio.RecordRealizedPnl(-fee);
            if (position is null)
            {
                position = new Position(Guid.NewGuid(), portfolio.Id, order.Symbol,
                    quantity, price, now, instrument.Id);
                tradingRepository.AddPosition(position);
            }
            else position.Add(quantity, price, now);
            realizedPnl = -fee;
        }
        else
        {
            if (position is null || position.Quantity < quantity)
                return new(OrderExecutionStatus.InsufficientQuantity, null, portfolio.CashBalance,
                    position?.Quantity ?? 0);
            var pricePnl = RoundMoney(AccountCurrency.PnlUsd(instrument, quantity,
                position.AverageEntryPrice, price));
            gross = RoundMoney(AccountCurrency.NotionalUsd(instrument, quantity,
                position.AverageEntryPrice) + pricePnl);
            fee = RoundMoney(gross * _options.FeeBps / 10_000m);
            realizedPnl = pricePnl - fee;
            if (gross <= fee)
                return Invalid();
            position.Sell(quantity, price, now);
            portfolio.Credit(gross - fee);
            portfolio.RecordRealizedPnl(realizedPnl);
            if (position.Quantity == 0) tradingRepository.RemovePosition(position);
        }

        order.Fill(price, now, quantity);
        tradingRepository.AddTrade(new Trade(Guid.NewGuid(), order.Id, portfolio.Id,
            order.Symbol, order.Side, quantity, price, gross, realizedPnl, now,
            instrument.Id, fee, order.Side == OrderSide.Buy
                ? quote.Ask ?? quote.CurrentPrice : quote.Bid ?? quote.CurrentPrice));
        return new(OrderExecutionStatus.Filled, MapOrder(order, instrument),
            portfolio.CashBalance, position.Quantity);
    }

    private async Task<bool> WithinRiskLimitsAsync(Portfolio portfolio, Order order,
        Instrument instrument, MarketQuote quote, CancellationToken token)
    {
        if (portfolio.MaxDailyLossPercent is null && portfolio.MaxPositionConcentrationPercent is null)
            return true;
        var positions = await tradingRepository.GetPositionsAsync(portfolio.Id, token);
        var values = new Dictionary<Guid, decimal>();
        foreach (var position in positions)
        {
            var mark = position.InstrumentId == instrument.Id ? quote :
                await marketDataService.GetQuoteAsync(position.Symbol, token)
                ?? throw new MarketDataUnavailableException($"A quote for {position.Symbol} is unavailable.");
            values[position.InstrumentId] = AccountCurrency.MarketValueUsd(position.Instrument,
                position.Quantity, position.AverageEntryPrice, mark.CurrentPrice);
        }
        var equity = portfolio.CashBalance + values.Values.Sum();
        if (portfolio.MaxDailyLossPercent is decimal dailyLimit)
        {
            var snapshots = await riskAnalytics.GetSnapshotsAsync(portfolio.Id, token);
            var today = DateTimeOffset.UtcNow.UtcDateTime.Date;
            var firstToday = snapshots.FirstOrDefault(x => x.RecordedAt.UtcDateTime.Date == today);
            var previous = snapshots.LastOrDefault(x => x.RecordedAt.UtcDateTime.Date < today);
            var start = firstToday?.Equity ?? previous?.Equity ?? portfolio.InitialBalance;
            if (firstToday is null && (previous is null ||
                DateTimeOffset.UtcNow - previous.RecordedAt > TimeSpan.FromDays(2)))
            {
                // An old observation cannot establish today's opening equity.
                start = equity;
                riskAnalytics.AddSnapshot(new PaperTrade.Domain.Portfolios.EquitySnapshot(
                    Guid.NewGuid(), portfolio.Id, DateTimeOffset.UtcNow,
                    decimal.Round(equity, 2), portfolio.CashBalance,
                    portfolio.RealizedPnl, decimal.Round(values.Values.Sum() -
                        positions.Sum(p => AccountCurrency.NotionalUsd(p.Instrument,
                            p.Quantity, p.AverageEntryPrice)), 2)));
            }
            if (start > 0 && equity <= start * (1 - dailyLimit / 100m)) return false;
        }
        if (portfolio.MaxPositionConcentrationPercent is decimal concentration)
        {
            var price = ExecutionPrice(order, quote, instrument);
            var added = RoundMoney(AccountCurrency.NotionalUsd(instrument, order.RemainingQuantity, price));
            var fee = RoundMoney(added * _options.FeeBps / 10_000m);
            var existing = values.GetValueOrDefault(instrument.Id);
            if (equity <= fee || (existing + added) / (equity - fee) * 100m > concentration) return false;
        }
        return true;
    }

    private void AddBracketChildren(Order parent, CreateOrderRequest request,
        Instrument instrument, DateTimeOffset now)
    {
        tradingRepository.AddOrder(new Order(Guid.NewGuid(), parent.PortfolioId,
            parent.Symbol, OrderSide.Sell, OrderType.Limit, parent.Quantity,
            request.TakeProfit!.Value, now, instrument.Id, parent.Id, request.ExpiresAt));
        tradingRepository.AddOrder(new Order(Guid.NewGuid(), parent.PortfolioId,
            parent.Symbol, OrderSide.Sell, OrderType.Stop, parent.Quantity,
            request.StopLoss!.Value, now, instrument.Id, parent.Id, request.ExpiresAt));
    }

    private decimal ExecutionPrice(Order order, MarketQuote quote, Instrument instrument)
    {
        var basePrice = order.Side == OrderSide.Buy
            ? quote.Ask ?? quote.CurrentPrice : quote.Bid ?? quote.CurrentPrice;
        var factor = order.Side == OrderSide.Buy
            ? 1 + _options.SlippageBps / 10_000m
            : 1 - _options.SlippageBps / 10_000m;
        var slipped = basePrice * factor;
        var rounded = order.Side == OrderSide.Buy
            ? decimal.Ceiling(slipped / instrument.TickSize) * instrument.TickSize
            : decimal.Floor(slipped / instrument.TickSize) * instrument.TickSize;
        if (order.Type == OrderType.Limit)
            rounded = order.Side == OrderSide.Buy
                ? Math.Min(rounded, order.RequestedPrice)
                : Math.Max(rounded, order.RequestedPrice);
        return rounded;
    }

    private static bool ShouldFill(Order order, MarketQuote quote)
    {
        var sidePrice = order.Side == OrderSide.Buy
            ? quote.Ask ?? quote.CurrentPrice : quote.Bid ?? quote.CurrentPrice;
        return order.Type switch
        {
            OrderType.Market or OrderType.Bracket => true,
            OrderType.Limit => order.Side == OrderSide.Buy
                ? sidePrice <= order.RequestedPrice : sidePrice >= order.RequestedPrice,
            OrderType.Stop => order.Side == OrderSide.Buy
                ? sidePrice >= order.RequestedPrice : sidePrice <= order.RequestedPrice,
            _ => false
        };
    }

    private static bool ValidPrices(CreateOrderRequest request, OrderType type, OrderSide side,
        MarketQuote quote, Instrument instrument)
    {
        if (request.ExpiresAt <= DateTimeOffset.UtcNow) return false;
        if (type is OrderType.Limit or OrderType.Stop)
            return ValidTick(request.Price, instrument) &&
                request.TakeProfit is null && request.StopLoss is null;
        if (request.Price is not null) return false;
        if (type == OrderType.Bracket)
            return side == OrderSide.Buy && ValidTick(request.TakeProfit, instrument) &&
                ValidTick(request.StopLoss, instrument) &&
                request.TakeProfit > (quote.Ask ?? quote.CurrentPrice) &&
                request.StopLoss < (quote.Bid ?? quote.CurrentPrice);
        return request.TakeProfit is null && request.StopLoss is null;
    }

    private static bool ValidTick(decimal? price, Instrument instrument) =>
        price is > 0 && price.Value % instrument.TickSize == 0;

    private bool ValidSimulationOptions() =>
        _options.FeeBps is >= 0 and <= 1000 &&
        _options.SlippageBps is >= 0 and <= 1000;

    private static decimal RoundMoney(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    private static OrderExecutionResult Invalid() =>
        new(OrderExecutionStatus.InvalidOrder, null, null, null);

    private static OrderDto MapOrder(Order order, Instrument? instrument = null)
    {
        var details = instrument ?? order.Instrument;
        return new OrderDto(order.Id, order.PortfolioId, order.Symbol,
            order.Side.ToString().ToLowerInvariant(), order.Type.ToString().ToLowerInvariant(),
            order.Quantity, order.RequestedPrice, order.ExecutedPrice,
            order.ExecutedPrice is null ? null :
                RoundMoney(AccountCurrency.NotionalUsd(details, order.FilledQuantity, order.ExecutedPrice.Value)),
            order.Status == OrderStatus.PartiallyFilled ? "partially_filled" :
                order.Status.ToString().ToLowerInvariant(), order.CreatedAt,
            order.ExecutedAt, order.InstrumentId, InstrumentDto.From(details),
            order.FilledQuantity, order.ParentOrderId, order.ExpiresAt, order.ClosedAt);
    }
}
