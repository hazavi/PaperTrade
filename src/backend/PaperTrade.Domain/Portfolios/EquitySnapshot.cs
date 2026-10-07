namespace PaperTrade.Domain.Portfolios;

public sealed class EquitySnapshot
{
    private EquitySnapshot() { }
    public EquitySnapshot(Guid id, Guid portfolioId, DateTimeOffset recordedAt,
        decimal equity, decimal cash, decimal realizedPnl, decimal unrealizedPnl)
    {
        Id = id;
        PortfolioId = portfolioId;
        RecordedAt = recordedAt;
        Equity = equity;
        Cash = cash;
        RealizedPnl = realizedPnl;
        UnrealizedPnl = unrealizedPnl;
    }
    public Guid Id { get; private set; }
    public Guid PortfolioId { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public decimal Equity { get; private set; }
    public decimal Cash { get; private set; }
    public decimal RealizedPnl { get; private set; }
    public decimal UnrealizedPnl { get; private set; }
    public Portfolio Portfolio { get; private set; } = null!;
}
