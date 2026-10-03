using PaperTrade.Domain.Portfolios;

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
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Order ID cannot be empty.", nameof(id));
        if (portfolioId == Guid.Empty) throw new ArgumentException("Portfolio ID cannot be empty.", nameof(portfolioId));
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (requestedPrice <= 0) throw new ArgumentOutOfRangeException(nameof(requestedPrice));

        Id = id;
        PortfolioId = portfolioId;
        Symbol = symbol.Trim().ToUpperInvariant();
        Side = side;
        Type = type;
        Quantity = quantity;
        RequestedPrice = requestedPrice;
        Status = OrderStatus.Pending;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid PortfolioId { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public OrderSide Side { get; private set; }
    public OrderType Type { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal RequestedPrice { get; private set; }
    public decimal? ExecutedPrice { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ExecutedAt { get; private set; }
    public Portfolio Portfolio { get; private set; } = null!;

    public void Fill(decimal executedPrice, DateTimeOffset executedAt)
    {
        if (Status != OrderStatus.Pending) throw new InvalidOperationException("Only pending orders can be filled.");
        if (executedPrice <= 0) throw new ArgumentOutOfRangeException(nameof(executedPrice));

        ExecutedPrice = executedPrice;
        ExecutedAt = executedAt;
        Status = OrderStatus.Filled;
    }

    public void Reject()
    {
        if (Status != OrderStatus.Pending) throw new InvalidOperationException("Only pending orders can be rejected.");
        Status = OrderStatus.Rejected;
    }
}
