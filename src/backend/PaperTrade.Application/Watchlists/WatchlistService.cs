using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Domain.Watchlists;

namespace PaperTrade.Application.Watchlists;

public sealed class WatchlistService(
    IWatchlistRepository watchlistRepository,
    IUnitOfWork unitOfWork)
    : IWatchlistService
{
    public async Task<IReadOnlyList<WatchlistDto>> GetAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var watchlists = await watchlistRepository.GetForUserAsync(
            userId,
            cancellationToken);

        return watchlists.Select(Map).ToArray();
    }

    public async Task<WatchlistChangeResult> CreateAsync(
        Guid userId,
        CreateWatchlistRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        if (await watchlistRepository.NameExistsAsync(
                userId,
                name,
                cancellationToken))
        {
            return new WatchlistChangeResult(
                WatchlistChangeStatus.Conflict,
                null);
        }

        var watchlist = new Watchlist(
            Guid.NewGuid(),
            userId,
            name,
            DateTimeOffset.UtcNow);

        watchlistRepository.Add(watchlist);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new WatchlistChangeResult(
            WatchlistChangeStatus.Success,
            Map(watchlist));
    }

    public async Task<WatchlistChangeResult> AddItemAsync(
        Guid userId,
        Guid watchlistId,
        AddWatchlistItemRequest request,
        CancellationToken cancellationToken)
    {
        var watchlist = await watchlistRepository.GetByIdAsync(
            watchlistId,
            userId,
            cancellationToken);

        if (watchlist is null)
        {
            return new WatchlistChangeResult(
                WatchlistChangeStatus.NotFound,
                null);
        }

        try
        {
            watchlist.AddItem(
                Guid.NewGuid(),
                request.Symbol,
                DateTimeOffset.UtcNow);
        }
        catch (InvalidOperationException)
        {
            return new WatchlistChangeResult(
                WatchlistChangeStatus.Conflict,
                null);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new WatchlistChangeResult(
            WatchlistChangeStatus.Success,
            Map(watchlist));
    }

    public async Task<WatchlistChangeStatus> RemoveItemAsync(
        Guid userId,
        Guid watchlistId,
        string symbol,
        CancellationToken cancellationToken)
    {
        var watchlist = await watchlistRepository.GetByIdAsync(
            watchlistId,
            userId,
            cancellationToken);

        if (watchlist is null || !watchlist.RemoveItem(symbol))
        {
            return WatchlistChangeStatus.NotFound;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return WatchlistChangeStatus.Success;
    }

    private static WatchlistDto Map(Watchlist watchlist)
    {
        return new WatchlistDto(
            watchlist.Id,
            watchlist.Name,
            watchlist.CreatedAt,
            watchlist.Items
                .OrderBy(item => item.Symbol)
                .Select(item => new WatchlistItemDto(
                    item.Id,
                    item.Symbol,
                    item.AddedAt))
                .ToArray());
    }
}
