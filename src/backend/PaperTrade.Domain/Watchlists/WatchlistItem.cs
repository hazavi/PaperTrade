namespace PaperTrade.Domain.Watchlists;

using PaperTrade.Domain.Instruments;

public sealed class WatchlistItem
{
    private WatchlistItem()
    {
    }

    public WatchlistItem(
        Guid id,
        Guid watchlistId,
        string symbol,
        DateTimeOffset addedAt,
        Guid instrumentId = default)
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
        InstrumentId = instrumentId;
        Symbol = symbol.Trim().ToUpperInvariant();
        AddedAt = addedAt;
    }

    public Guid Id { get; private set; }

    public Guid WatchlistId { get; private set; }
    public Guid InstrumentId { get; private set; }
    public Instrument Instrument { get; private set; } = null!;

    public string Symbol { get; private set; } = string.Empty;

    public DateTimeOffset AddedAt { get; private set; }

    public Watchlist Watchlist { get; private set; } = null!;
}
