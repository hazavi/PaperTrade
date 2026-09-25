using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Domain.Portfolios;

namespace PaperTrade.Infrastructure.Persistence.Repositories;

internal sealed class PortfolioRepository(PaperTradeDbContext dbContext)
    : IPortfolioRepository
{
    public void Add(Portfolio portfolio)
    {
        dbContext.Portfolios.Add(portfolio);
    }
}