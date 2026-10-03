namespace PaperTrade.Application.Portfolios;

public sealed record PortfolioDto(
    Guid Id,
    string Name,
    decimal CashBalance,
    decimal InitialBalance,
    decimal MarketValue,
    decimal PortfolioValue,
    decimal UnrealizedPnl,
    decimal RealizedPnl,
    decimal TotalReturnPercentage,
    IReadOnlyList<PositionDto> Positions);

public sealed record PositionDto(
    Guid Id,
    string Symbol,
    decimal Quantity,
    decimal AverageEntryPrice,
    decimal CurrentPrice,
    decimal MarketValue,
    decimal UnrealizedPnl,
    decimal ReturnPercentage,
    DateTimeOffset UpdatedAt);
