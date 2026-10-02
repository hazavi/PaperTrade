namespace PaperTrade.Application.Watchlists;

public interface IWatchlistService
{
    Task<IReadOnlyList<WatchlistDto>> GetAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<WatchlistChangeResult> CreateAsync(
        Guid userId,
        CreateWatchlistRequest request,
        CancellationToken cancellationToken);

    Task<WatchlistChangeResult> AddItemAsync(
        Guid userId,
        Guid watchlistId,
        AddWatchlistItemRequest request,
        CancellationToken cancellationToken);

    Task<WatchlistChangeStatus> RemoveItemAsync(
        Guid userId,
        Guid watchlistId,
        string symbol,
        CancellationToken cancellationToken);
}
