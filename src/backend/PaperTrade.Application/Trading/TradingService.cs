using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Application.Markets;
using PaperTrade.Domain.Orders;
using PaperTrade.Domain.Positions;
using PaperTrade.Domain.Trades;
using PaperTrade.Domain.Instruments;

namespace PaperTrade.Application.Trading;

public sealed class TradingService(
    IPortfolioRepository portfolioRepository,
    ITradingRepository tradingRepository,
    IUnitOfWork unitOfWork,
    IMarketDataService marketDataService,
    IInstrumentCatalog instrumentCatalog)
    : ITradingService
{
    public async Task<OrderExecutionResult> PlaceOrderAsync(
        Guid userId,
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrderSide>(request.Side, true, out var side) ||
            !Enum.TryParse<OrderType>(request.Type, true, out var type) ||
            type != OrderType.Market)
        {
            return new(OrderExecutionStatus.InvalidOrder, null, null, null);
        }

        var symbol = request.Symbol.Trim().ToUpperInvariant();
        var quote = await marketDataService.GetQuoteAsync(symbol, cancellationToken);
        if (quote is null)
        {
            return new(OrderExecutionStatus.QuoteNotFound, null, null, null);
        }

        var instrument = await instrumentCatalog.GetOrCreateAsync(symbol, cancellationToken);
        if (!instrument.IsTradable ||
            request.Quantity < instrument.MinimumOrderSize ||
            decimal.Round(request.Quantity, instrument.QuantityPrecision) != request.Quantity ||
            request.Quantity % instrument.MinimumOrderSize != 0)
        {
            return new(OrderExecutionStatus.InvalidOrder, null, null, null);
        }

        var executionPrice = decimal.Round(quote.CurrentPrice / instrument.TickSize,
            0, MidpointRounding.AwayFromZero) * instrument.TickSize;
        var totalValue = RoundMoney(AccountCurrency.NotionalUsd(instrument, request.Quantity, executionPrice));
        var now = DateTimeOffset.UtcNow;

        return await unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var portfolio = await portfolioRepository.GetByUserIdAsync(userId, token);
                if (portfolio is null)
                {
                    return new OrderExecutionResult(
                        OrderExecutionStatus.PortfolioNotFound, null, null, null);
                }

                var position = await tradingRepository.GetPositionAsync(
                    portfolio.Id, instrument.Id, token);

                if (side == OrderSide.Buy && totalValue > portfolio.CashBalance)
                {
                    return new(OrderExecutionStatus.InsufficientFunds, null,
                        portfolio.CashBalance, position?.Quantity ?? 0);
                }

                if (side == OrderSide.Sell &&
                    (position is null || request.Quantity > position.Quantity))
                {
                    return new(OrderExecutionStatus.InsufficientQuantity, null,
                        portfolio.CashBalance, position?.Quantity ?? 0);
                }

                var order = new Order(Guid.NewGuid(), portfolio.Id, symbol,
                    side, type, request.Quantity, executionPrice, now, instrument.Id);
                decimal realizedPnl = 0;

                if (side == OrderSide.Buy)
                {
                    portfolio.Debit(totalValue);
                    if (position is null)
                    {
                        position = new Position(Guid.NewGuid(), portfolio.Id,
                            symbol, request.Quantity, executionPrice, now, instrument.Id);
                        tradingRepository.AddPosition(position);
                    }
                    else
                    {
                        position.Add(request.Quantity, executionPrice, now);
                    }
                }
                else
                {
                    var entryPrice = position!.AverageEntryPrice;
                    realizedPnl = RoundMoney(AccountCurrency.PnlUsd(instrument,
                        request.Quantity, entryPrice, executionPrice));
                    totalValue = RoundMoney(AccountCurrency.NotionalUsd(instrument,
                        request.Quantity, entryPrice) + realizedPnl);
                    if (totalValue <= 0)
                        return new(OrderExecutionStatus.InvalidOrder, null,
                            portfolio.CashBalance, position.Quantity);
                    position.Sell(request.Quantity, executionPrice, now);
                    portfolio.Credit(totalValue);
                    portfolio.RecordRealizedPnl(realizedPnl);
                    if (position.Quantity == 0)
                    {
                        tradingRepository.RemovePosition(position);
                    }
                }

                order.Fill(executionPrice, now);
                tradingRepository.AddOrder(order);
                tradingRepository.AddTrade(new Trade(Guid.NewGuid(), order.Id,
                    portfolio.Id, symbol, side, request.Quantity, executionPrice,
                    totalValue, realizedPnl, now, instrument.Id));

                return new OrderExecutionResult(OrderExecutionStatus.Filled,
                    MapOrder(order, instrument), portfolio.CashBalance,
                    position.Quantity);
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<OrderDto>> GetOrdersAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var portfolio = await portfolioRepository.GetByUserIdAsync(userId, cancellationToken);
        if (portfolio is null) return [];

        var orders = await tradingRepository.GetOrdersAsync(portfolio.Id, cancellationToken);
        return orders.Select(order => MapOrder(order)).ToArray();
    }

    private static decimal RoundMoney(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    private static OrderDto MapOrder(Order order, Instrument? instrument = null)
    {
        return new OrderDto(order.Id, order.PortfolioId, order.Symbol,
            order.Side.ToString().ToLowerInvariant(),
            order.Type.ToString().ToLowerInvariant(), order.Quantity,
            order.RequestedPrice, order.ExecutedPrice,
            order.ExecutedPrice is null
                ? null
                : RoundMoney(order.ExecutedPrice.Value * order.Quantity),
            order.Status.ToString().ToLowerInvariant(), order.CreatedAt,
            order.ExecutedAt, order.InstrumentId,
            instrument is null && order.Instrument is null ? null :
                InstrumentDto.From(instrument ?? order.Instrument));
    }
}
