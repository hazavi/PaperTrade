namespace PaperTrade.Domain.Charts;

public sealed class ChartLayout
{
    private ChartLayout() { }
    public ChartLayout(Guid id, Guid userId, string name, string symbol,
        string timeframe, string stateJson, DateTimeOffset updatedAt)
    {
        Id = id;
        UserId = userId;
        Update(name, symbol, timeframe, stateJson, updatedAt);
    }
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Symbol { get; private set; } = string.Empty;
    public string Timeframe { get; private set; } = string.Empty;
    public string StateJson { get; private set; } = "{}";
    public DateTimeOffset UpdatedAt { get; private set; }
    public void Update(string name, string symbol, string timeframe,
        string stateJson, DateTimeOffset updatedAt)
    {
        Name = name.Trim();
        Symbol = symbol.Trim().ToUpperInvariant();
        Timeframe = timeframe.Trim().ToUpperInvariant();
        StateJson = stateJson;
        UpdatedAt = updatedAt;
    }
}
