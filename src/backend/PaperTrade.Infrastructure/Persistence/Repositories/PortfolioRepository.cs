using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Domain.Portfolios;
using Microsoft.EntityFrameworkCore;

namespace PaperTrade.Infrastructure.Persistence.Repositories;

internal sealed class PortfolioRepository(PaperTradeDbContext dbContext)
    : IPortfolioRepository
{
    public void Add(Portfolio portfolio)
    {
        dbContext.Portfolios.Add(portfolio);
        dbContext.EquitySnapshots.Add(new EquitySnapshot(Guid.NewGuid(), portfolio.Id,
            portfolio.CreatedAt, portfolio.InitialBalance, portfolio.InitialBalance, 0, 0));
    }

    public Task<Portfolio?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.Portfolios.SingleOrDefaultAsync(
            portfolio => portfolio.UserId == userId,
            cancellationToken);
    }

    public Task<Portfolio?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Portfolios.SingleOrDefaultAsync(portfolio => portfolio.Id == id, cancellationToken);
}
