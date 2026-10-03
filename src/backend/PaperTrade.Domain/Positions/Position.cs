using PaperTrade.Domain.Portfolios;

namespace PaperTrade.Domain.Positions;

public sealed class Position
{
    private Position()
    {
    }

    public Position(Guid id, Guid portfolioId, string symbol, decimal quantity,
        decimal averageEntryPrice, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Position ID cannot be empty.", nameof(id));
        if (portfolioId == Guid.Empty) throw new ArgumentException("Portfolio ID cannot be empty.", nameof(portfolioId));
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (averageEntryPrice <= 0) throw new ArgumentOutOfRangeException(nameof(averageEntryPrice));

        Id = id;
        PortfolioId = portfolioId;
        Symbol = symbol.Trim().ToUpperInvariant();
        Quantity = quantity;
        AverageEntryPrice = averageEntryPrice;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid PortfolioId { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal AverageEntryPrice { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Portfolio Portfolio { get; private set; } = null!;

    public void Add(decimal quantity, decimal price, DateTimeOffset updatedAt)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (price <= 0) throw new ArgumentOutOfRangeException(nameof(price));

        var totalCost = Quantity * AverageEntryPrice + quantity * price;
        Quantity += quantity;
        AverageEntryPrice = totalCost / Quantity;
        UpdatedAt = updatedAt;
    }

    public decimal Sell(decimal quantity, decimal price, DateTimeOffset updatedAt)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (price <= 0) throw new ArgumentOutOfRangeException(nameof(price));
        if (quantity > Quantity) throw new InvalidOperationException("Cannot sell more shares than are owned.");

        var realizedPnl = (price - AverageEntryPrice) * quantity;
        Quantity -= quantity;
        UpdatedAt = updatedAt;
        return realizedPnl;
    }
}
