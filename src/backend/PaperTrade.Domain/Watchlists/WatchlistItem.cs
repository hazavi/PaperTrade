namespace PaperTrade.Domain.Watchlists;

public sealed class WatchlistItem
{
    private WatchlistItem()
    {
    }

    public WatchlistItem(
        Guid id,
        Guid watchlistId,
        string symbol,
        DateTimeOffset addedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Watchlist item ID cannot be empty.",
                nameof(id));
        }

        if (watchlistId == Guid.Empty)
        {
            throw new ArgumentException(
                "Watchlist ID cannot be empty.",
                nameof(watchlistId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        Id = id;
        WatchlistId = watchlistId;
        Symbol = symbol.Trim().ToUpperInvariant();
        AddedAt = addedAt;
    }

    public Guid Id { get; private set; }

    public Guid WatchlistId { get; private set; }

    public string Symbol { get; private set; } = string.Empty;

    public DateTimeOffset AddedAt { get; private set; }

    public Watchlist Watchlist { get; private set; } = null!;
}
