using PaperTrade.Domain.Portfolios;

namespace PaperTrade.Application.Abstractions.Persistence;

public interface IPortfolioRepository
{
    void Add(Portfolio portfolio);

    Task<Portfolio?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
