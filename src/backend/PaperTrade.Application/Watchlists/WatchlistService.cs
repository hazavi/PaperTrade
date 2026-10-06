using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Domain.Watchlists;
using PaperTrade.Application.Markets;
using PaperTrade.Domain.Instruments;

namespace PaperTrade.Application.Watchlists;

public sealed class WatchlistService(
    IWatchlistRepository watchlistRepository,
    IUnitOfWork unitOfWork,
    IInstrumentCatalog instrumentCatalog)
    : IWatchlistService
{
    public async Task<IReadOnlyList<WatchlistDto>> GetAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var watchlists = await watchlistRepository.GetForUserAsync(
            userId,
            cancellationToken);

        return watchlists.Select(watchlist => Map(watchlist)).ToArray();
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
            var instrument = await instrumentCatalog.GetOrCreateAsync(request.Symbol, cancellationToken);
            watchlist.AddItem(
                Guid.NewGuid(),
                instrument.Symbol,
                DateTimeOffset.UtcNow,
                instrument.Id);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return new WatchlistChangeResult(
                WatchlistChangeStatus.Success, Map(watchlist, instrument));
        }
        catch (InvalidOperationException)
        {
            return new WatchlistChangeResult(
                WatchlistChangeStatus.Conflict,
                null);
        }

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

    private static WatchlistDto Map(Watchlist watchlist, Instrument? newInstrument = null)
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
                    item.AddedAt,
                    item.InstrumentId,
                    item.Instrument is not null ? InstrumentDto.From(item.Instrument) :
                        newInstrument is not null && newInstrument.Id == item.InstrumentId ?
                            InstrumentDto.From(newInstrument) : null))
                .ToArray());
    }
}
