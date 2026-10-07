namespace PaperTrade.Domain.TradingJournal;

public sealed class JournalEntry
{
    private JournalEntry() { }
    public JournalEntry(Guid id, Guid portfolioId, Guid orderId, string note, DateTimeOffset createdAt)
    {
        Id = id;
        PortfolioId = portfolioId;
        OrderId = orderId;
        Update(note);
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }
    public Guid Id { get; private set; }
    public Guid PortfolioId { get; private set; }
    public Guid OrderId { get; private set; }
    public string Note { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public void Update(string note)
    {
        if (string.IsNullOrWhiteSpace(note) || note.Length > 4000)
            throw new ArgumentException("Journal note must be 1 to 4000 characters.", nameof(note));
        Note = note.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
