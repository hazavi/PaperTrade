using PaperTrade.Domain.Users;
using PaperTrade.Domain.Instruments;

namespace PaperTrade.Domain.Alerts;

public sealed class PriceAlert
{
    private PriceAlert() { }

    public PriceAlert(Guid id, Guid userId, string symbol,
        PriceAlertDirection direction, decimal targetPrice,
        DateTimeOffset createdAt,
        Guid instrumentId = default, string metric = "price")
    {
        if (id == Guid.Empty) throw new ArgumentException("Alert ID cannot be empty.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        if (metric == "percentChange" ? targetPrice is < -100 or > 100 : targetPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetPrice));
        Id = id;
        UserId = userId;
        InstrumentId = instrumentId;
        Symbol = symbol.Trim().ToUpperInvariant();
        Direction = direction;
        Metric = metric;
        TargetPrice = targetPrice;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid InstrumentId { get; private set; }
    public Instrument Instrument { get; private set; } = null!;
    public string Symbol { get; private set; } = string.Empty;
    public PriceAlertDirection Direction { get; private set; }
    public string Metric { get; private set; } = "price";
    public decimal TargetPrice { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? TriggeredAt { get; private set; }
    public User User { get; private set; } = null!;

    public bool ShouldTrigger(decimal currentPrice) =>
        IsActive && (Direction == PriceAlertDirection.Above
            ? currentPrice >= TargetPrice
            : currentPrice <= TargetPrice);

    public void Trigger(DateTimeOffset triggeredAt)
    {
        if (!IsActive) throw new InvalidOperationException("Alert is not active.");
        IsActive = false;
        TriggeredAt = triggeredAt;
    }
}
