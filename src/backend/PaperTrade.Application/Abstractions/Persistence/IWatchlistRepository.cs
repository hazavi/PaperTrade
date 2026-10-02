using PaperTrade.Domain.Watchlists;

namespace PaperTrade.Application.Abstractions.Persistence;

public interface IWatchlistRepository
{
    Task<IReadOnlyList<Watchlist>> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<Watchlist?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(
        Guid userId,
        string name,
        CancellationToken cancellationToken);

    void Add(Watchlist watchlist);
}
