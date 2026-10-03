using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Application.Markets;
using PaperTrade.Domain.Positions;

namespace PaperTrade.Application.Portfolios;

public sealed class PortfolioService(
    IPortfolioRepository portfolioRepository,
    ITradingRepository tradingRepository,
    IMarketDataService marketDataService)
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
        var marketValue = RoundMoney(quote.CurrentPrice * position.Quantity);
        var costBasis = RoundMoney(position.AverageEntryPrice * position.Quantity);
        var unrealizedPnl = RoundMoney(marketValue - costBasis);
        var returnPercentage = costBasis == 0
            ? 0
            : decimal.Round(unrealizedPnl / costBasis * 100, 2,
                MidpointRounding.AwayFromZero);

        return new PositionDto(position.Id, position.Symbol, position.Quantity,
            position.AverageEntryPrice, quote.CurrentPrice, marketValue,
            unrealizedPnl, returnPercentage, position.UpdatedAt);
    }

    private static decimal RoundMoney(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
