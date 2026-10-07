using PaperTrade.Application.Markets;

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
    IReadOnlyList<PositionDto> Positions,
    decimal UsedMargin = 0,
    decimal AvailableMargin = 0,
    decimal? MarginLevelPercent = null,
    bool MaintenanceWarning = false,
    decimal GrossExposure = 0);

public sealed record PositionDto(
    Guid Id,
    string Symbol,
    decimal Quantity,
    decimal AverageEntryPrice,
    decimal CurrentPrice,
    decimal MarketValue,
    decimal UnrealizedPnl,
    decimal ReturnPercentage,
    DateTimeOffset UpdatedAt,
    Guid InstrumentId = default,
    InstrumentDto? Instrument = null,
    decimal MarginReserved = 0,
    decimal NotionalValue = 0);
