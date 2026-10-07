using PaperTrade.Domain.Portfolios;
using PaperTrade.Domain.Instruments;

namespace PaperTrade.Domain.Orders;

public sealed class Order
{
    private Order()
    {
    }

    public Order(
        Guid id,
        Guid portfolioId,
        string symbol,
        OrderSide side,
        OrderType type,
        decimal quantity,
        decimal requestedPrice,
        DateTimeOffset createdAt,
        Guid instrumentId = default,
        Guid? parentOrderId = null,
        DateTimeOffset? expiresAt = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Order ID cannot be empty.", nameof(id));
        if (portfolioId == Guid.Empty) throw new ArgumentException("Portfolio ID cannot be empty.", nameof(portfolioId));
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (requestedPrice <= 0) throw new ArgumentOutOfRangeException(nameof(requestedPrice));

        Id = id;
        PortfolioId = portfolioId;
        InstrumentId = instrumentId;
        Symbol = symbol.Trim().ToUpperInvariant();
        Side = side;
        Type = type;
        Quantity = quantity;
        RequestedPrice = requestedPrice;
        ParentOrderId = parentOrderId;
        ExpiresAt = expiresAt;
        Status = OrderStatus.Pending;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid PortfolioId { get; private set; }
    public Guid InstrumentId { get; private set; }
    public Instrument Instrument { get; private set; } = null!;
    public string Symbol { get; private set; } = string.Empty;
    public OrderSide Side { get; private set; }
    public OrderType Type { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal RequestedPrice { get; private set; }
    public decimal? ExecutedPrice { get; private set; }
    public decimal FilledQuantity { get; private set; }
    public Guid? ParentOrderId { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ExecutedAt { get; private set; }
    public Portfolio Portfolio { get; private set; } = null!;

    public decimal RemainingQuantity => Quantity - FilledQuantity;

    public void Fill(decimal executedPrice, DateTimeOffset executedAt, decimal? filledQuantity = null)
    {
        if (Status is not (OrderStatus.Pending or OrderStatus.PartiallyFilled))
            throw new InvalidOperationException("Only open orders can be filled.");
        if (executedPrice <= 0) throw new ArgumentOutOfRangeException(nameof(executedPrice));
        var amount = filledQuantity ?? RemainingQuantity;
        if (amount <= 0 || amount > RemainingQuantity) throw new ArgumentOutOfRangeException(nameof(filledQuantity));

        ExecutedPrice = ((ExecutedPrice ?? 0) * FilledQuantity + executedPrice * amount) /
            (FilledQuantity + amount);
        FilledQuantity += amount;
        ExecutedAt = executedAt;
        Status = FilledQuantity == Quantity ? OrderStatus.Filled : OrderStatus.PartiallyFilled;
        if (Status == OrderStatus.Filled) ClosedAt = executedAt;
    }

    public void Reject()
    {
        if (Status is not (OrderStatus.Pending or OrderStatus.PartiallyFilled))
            throw new InvalidOperationException("Only open orders can be rejected.");
        Status = OrderStatus.Rejected;
        ClosedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status is not (OrderStatus.Pending or OrderStatus.PartiallyFilled))
            throw new InvalidOperationException("Only open orders can be cancelled.");
        Status = OrderStatus.Cancelled;
        ClosedAt = now;
    }

    public void Expire(DateTimeOffset now)
    {
        if (Status is not (OrderStatus.Pending or OrderStatus.PartiallyFilled))
            throw new InvalidOperationException("Only open orders can expire.");
        Status = OrderStatus.Expired;
        ClosedAt = now;
    }
}
