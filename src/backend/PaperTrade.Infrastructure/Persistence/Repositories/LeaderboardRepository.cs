using Microsoft.EntityFrameworkCore;
using PaperTrade.Application.Abstractions.Persistence;

namespace PaperTrade.Infrastructure.Persistence.Repositories;

internal sealed class LeaderboardRepository(PaperTradeDbContext dbContext)
    : ILeaderboardRepository
{
    public async Task<IReadOnlyList<LeaderboardAccount>> GetAccountsAsync(CancellationToken cancellationToken)
    {
        var portfolios = await dbContext.Portfolios.AsNoTracking()
            .Include(portfolio => portfolio.User)
            .ToListAsync(cancellationToken);
        var positions = await dbContext.Positions.AsNoTracking()
            .ToListAsync(cancellationToken);
        var byPortfolio = positions.ToLookup(position => position.PortfolioId);

        return portfolios.Select(portfolio => new LeaderboardAccount(
            portfolio.UserId,
            portfolio.User.DisplayName,
            portfolio.CashBalance,
            portfolio.InitialBalance,
            byPortfolio[portfolio.Id].Select(position =>
                new LeaderboardPosition(position.Symbol, position.Quantity)).ToArray()))
            .ToArray();
    }
}
