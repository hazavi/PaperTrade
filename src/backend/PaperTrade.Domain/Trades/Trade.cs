using PaperTrade.Domain.Orders;
using PaperTrade.Domain.Portfolios;
using PaperTrade.Domain.Instruments;

namespace PaperTrade.Domain.Trades;

public sealed class Trade
{
    private Trade()
    {
    }

    public Trade(Guid id, Guid orderId, Guid portfolioId, string symbol,
        OrderSide side, decimal quantity, decimal price, decimal totalValue,
        decimal realizedPnl, DateTimeOffset executedAt,
        Guid instrumentId = default)
    {
        if (id == Guid.Empty) throw new ArgumentException("Trade ID cannot be empty.", nameof(id));
        if (orderId == Guid.Empty) throw new ArgumentException("Order ID cannot be empty.", nameof(orderId));
        if (portfolioId == Guid.Empty) throw new ArgumentException("Portfolio ID cannot be empty.", nameof(portfolioId));
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (price <= 0) throw new ArgumentOutOfRangeException(nameof(price));
        if (totalValue <= 0) throw new ArgumentOutOfRangeException(nameof(totalValue));

        Id = id;
        OrderId = orderId;
        PortfolioId = portfolioId;
        InstrumentId = instrumentId;
        Symbol = symbol.Trim().ToUpperInvariant();
        Side = side;
        Quantity = quantity;
        Price = price;
        TotalValue = totalValue;
        RealizedPnl = realizedPnl;
        ExecutedAt = executedAt;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid PortfolioId { get; private set; }
    public Guid InstrumentId { get; private set; }
    public Instrument Instrument { get; private set; } = null!;
    public string Symbol { get; private set; } = string.Empty;
    public OrderSide Side { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal Price { get; private set; }
    public decimal TotalValue { get; private set; }
    public decimal RealizedPnl { get; private set; }
    public DateTimeOffset ExecutedAt { get; private set; }
    public Order Order { get; private set; } = null!;
    public Portfolio Portfolio { get; private set; } = null!;
}
