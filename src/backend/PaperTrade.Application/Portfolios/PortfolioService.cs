using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Application.Markets;
using PaperTrade.Domain.Positions;

namespace PaperTrade.Application.Portfolios;

public sealed class PortfolioService(
    IPortfolioRepository portfolioRepository,
    ITradingRepository tradingRepository,
    IMarketDataService marketDataService,
    IRiskAnalyticsRepository analyticsRepository,
    IUnitOfWork unitOfWork)
    : IPortfolioService
{
    public async Task<PortfolioDto?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var portfolio = await portfolioRepository.GetByUserIdAsync(userId, cancellationToken);
        if (portfolio is null) return null;

        var positions = await tradingRepository.GetPositionsAsync(portfolio.Id, cancellationToken);
        var positionDtos = await Task.WhenAll(positions.Select(position =>
            MapPositionAsync(position, cancellationToken)));

        var marketValue = RoundMoney(positionDtos.Sum(position => position.MarketValue));
        var unrealizedPnl = RoundMoney(positionDtos.Sum(position => position.UnrealizedPnl));
        var portfolioValue = RoundMoney(portfolio.CashBalance + marketValue);
        var latest = await analyticsRepository.GetLatestSnapshotAsync(portfolio.Id, cancellationToken);
        if (latest is null || latest.Cash != portfolio.CashBalance ||
            latest.RealizedPnl != portfolio.RealizedPnl ||
            (DateTimeOffset.UtcNow - latest.RecordedAt >= TimeSpan.FromMinutes(5) &&
            (latest.Equity != portfolioValue || DateTimeOffset.UtcNow - latest.RecordedAt >= TimeSpan.FromHours(1))))
        {
            analyticsRepository.AddSnapshot(new PaperTrade.Domain.Portfolios.EquitySnapshot(Guid.NewGuid(),
                portfolio.Id, DateTimeOffset.UtcNow, portfolioValue, portfolio.CashBalance,
                portfolio.RealizedPnl, unrealizedPnl));
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        var totalReturn = portfolio.InitialBalance == 0
            ? 0
            : decimal.Round(
                (portfolioValue - portfolio.InitialBalance) /
                portfolio.InitialBalance * 100,
                2,
                MidpointRounding.AwayFromZero);

        return new PortfolioDto(portfolio.Id, portfolio.Name,
            portfolio.CashBalance, portfolio.InitialBalance, marketValue,
            portfolioValue, unrealizedPnl, portfolio.RealizedPnl,
            totalReturn, positionDtos);
    }

    private async Task<PositionDto> MapPositionAsync(
        Position position,
        CancellationToken cancellationToken)
    {
        var quote = await marketDataService.GetQuoteAsync(position.Symbol, cancellationToken)
            ?? throw new MarketDataUnavailableException(
                $"A quote for {position.Symbol} is unavailable.");
        var marketValue = RoundMoney(AccountCurrency.MarketValueUsd(position.Instrument,
            position.Quantity, position.AverageEntryPrice, quote.CurrentPrice));
        var costBasis = RoundMoney(AccountCurrency.NotionalUsd(position.Instrument,
            position.Quantity, position.AverageEntryPrice));
        var unrealizedPnl = RoundMoney(marketValue - costBasis);
        var returnPercentage = costBasis == 0
            ? 0
            : decimal.Round(unrealizedPnl / costBasis * 100, 2,
                MidpointRounding.AwayFromZero);

        return new PositionDto(position.Id, position.Symbol, position.Quantity,
            position.AverageEntryPrice, quote.CurrentPrice, marketValue,
            unrealizedPnl, returnPercentage, position.UpdatedAt, position.InstrumentId,
            InstrumentDto.From(position.Instrument));
    }

    private static decimal RoundMoney(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
