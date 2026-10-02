namespace PaperTrade.Application.Watchlists;

public sealed record CreateWatchlistRequest(string Name);

public sealed record AddWatchlistItemRequest(string Symbol);

public sealed record WatchlistDto(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    IReadOnlyList<WatchlistItemDto> Items);

public sealed record WatchlistItemDto(
    Guid Id,
    string Symbol,
    DateTimeOffset AddedAt);

public enum WatchlistChangeStatus
{
    Success,
    NotFound,
    Conflict
}

public sealed record WatchlistChangeResult(
    WatchlistChangeStatus Status,
    WatchlistDto? Watchlist);
