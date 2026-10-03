namespace PaperTrade.Application.Portfolios;

public interface IPortfolioService
{
    Task<PortfolioDto?> GetAsync(Guid userId, CancellationToken cancellationToken);
}
