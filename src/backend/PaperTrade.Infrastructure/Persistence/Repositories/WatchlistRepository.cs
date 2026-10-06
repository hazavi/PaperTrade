using Microsoft.EntityFrameworkCore;
using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Domain.Watchlists;

namespace PaperTrade.Infrastructure.Persistence.Repositories;

internal sealed class WatchlistRepository(
    PaperTradeDbContext dbContext)
    : IWatchlistRepository
{
    public async Task<IReadOnlyList<Watchlist>> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Watchlists
            .AsNoTracking()
            .Include(watchlist => watchlist.Items)
            .ThenInclude(item => item.Instrument)
            .Where(watchlist => watchlist.UserId == userId)
            .OrderBy(watchlist => watchlist.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Watchlist?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.Watchlists
            .Include(watchlist => watchlist.Items)
            .ThenInclude(item => item.Instrument)
            .SingleOrDefaultAsync(
                watchlist =>
                    watchlist.Id == id &&
                    watchlist.UserId == userId,
                cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        Guid userId,
        string name,
        CancellationToken cancellationToken)
    {
        return dbContext.Watchlists.AnyAsync(
            watchlist =>
                watchlist.UserId == userId &&
                watchlist.Name == name,
            cancellationToken);
    }

    public void Add(Watchlist watchlist)
    {
        dbContext.Watchlists.Add(watchlist);
    }
}
