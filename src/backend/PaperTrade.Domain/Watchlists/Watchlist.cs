using PaperTrade.Domain.Users;

namespace PaperTrade.Domain.Watchlists;

public sealed class Watchlist
{
    private readonly List<WatchlistItem> _items = [];

    private Watchlist()
    {
    }

    public Watchlist(
        Guid id,
        Guid userId,
        string name,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Watchlist ID cannot be empty.",
                nameof(id));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID cannot be empty.",
                nameof(userId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        UserId = userId;
        Name = name.Trim();
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public User User { get; private set; } = null!;

    public IReadOnlyCollection<WatchlistItem> Items => _items;

    public WatchlistItem AddItem(
        Guid itemId,
        string symbol,
        DateTimeOffset addedAt,
        Guid instrumentId = default)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);

        if (_items.Any(item => item.Symbol == normalizedSymbol ||
            (instrumentId != Guid.Empty && item.InstrumentId == instrumentId)))
        {
            throw new InvalidOperationException(
                $"{normalizedSymbol} is already in this watchlist.");
        }

        var item = new WatchlistItem(
            itemId,
            Id,
            normalizedSymbol,
            addedAt,
            instrumentId);

        _items.Add(item);
        return item;
    }

    public bool RemoveItem(string symbol)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        var item = _items.SingleOrDefault(
            candidate => candidate.Symbol == normalizedSymbol);

        return item is not null && _items.Remove(item);
    }

    private static string NormalizeSymbol(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        return symbol.Trim().ToUpperInvariant();
    }
}
