using PaperTrade.Domain.Users;

namespace PaperTrade.Domain.Alerts;

public sealed class PriceAlert
{
    private PriceAlert() { }

    public PriceAlert(Guid id, Guid userId, string symbol,
        PriceAlertDirection direction, decimal targetPrice,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Alert ID cannot be empty.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        if (targetPrice <= 0) throw new ArgumentOutOfRangeException(nameof(targetPrice));
        Id = id;
        UserId = userId;
        Symbol = symbol.Trim().ToUpperInvariant();
        Direction = direction;
        TargetPrice = targetPrice;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public PriceAlertDirection Direction { get; private set; }
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
