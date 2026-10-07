using PaperTrade.Application.Markets;

namespace PaperTrade.Application.Engagement;

public sealed record CreatePriceAlertRequest(
    string Symbol,
    string Direction,
    decimal TargetPrice,
    string Metric = "price");

public sealed record PriceAlertDto(
    Guid Id,
    string Symbol,
    string Direction,
    decimal TargetPrice,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? TriggeredAt,
    Guid InstrumentId = default,
    InstrumentDto? Instrument = null,
    string Metric = "price");

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    bool IsRead,
    DateTimeOffset CreatedAt);
