namespace PaperTrade.Domain.Portfolios;

public sealed class FinancingCharge
{
    private FinancingCharge() { }
    public FinancingCharge(Guid id, Guid portfolioId, Guid positionId, string symbol,
        DateTimeOffset chargedAt, decimal amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Id = id;
        PortfolioId = portfolioId;
        PositionId = positionId;
        Symbol = symbol;
        ChargedAt = chargedAt;
        Amount = amount;
    }
    public Guid Id { get; private set; }
    public Guid PortfolioId { get; private set; }
    public Guid PositionId { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public DateTimeOffset ChargedAt { get; private set; }
    public decimal Amount { get; private set; }
}
