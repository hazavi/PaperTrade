using PaperTrade.Application.Abstractions.Persistence;

namespace PaperTrade.Infrastructure.Persistence;

internal sealed class UnitOfWork(PaperTradeDbContext dbContext)
    : IUnitOfWork
{
    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}