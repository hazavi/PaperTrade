using Microsoft.EntityFrameworkCore;
using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Domain.Orders;
using PaperTrade.Domain.Positions;
using PaperTrade.Domain.Trades;

namespace PaperTrade.Infrastructure.Persistence.Repositories;

internal sealed class TradingRepository(PaperTradeDbContext dbContext)
    : ITradingRepository
{
    public Task<Position?> GetPositionAsync(Guid portfolioId, string symbol, CancellationToken cancellationToken)
    {
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        return dbContext.Positions.SingleOrDefaultAsync(
            position => position.PortfolioId == portfolioId && position.Symbol == normalizedSymbol,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Position>> GetPositionsAsync(Guid portfolioId, CancellationToken cancellationToken)
    {
        return await dbContext.Positions.AsNoTracking()
            .Where(position => position.PortfolioId == portfolioId)
            .OrderBy(position => position.Symbol)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetOrdersAsync(Guid portfolioId, CancellationToken cancellationToken)
    {
        return await dbContext.Orders.AsNoTracking()
            .Where(order => order.PortfolioId == portfolioId)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public void AddPosition(Position position) => dbContext.Positions.Add(position);
    public void RemovePosition(Position position) => dbContext.Positions.Remove(position);
    public void AddOrder(Order order) => dbContext.Orders.Add(order);
    public void AddTrade(Trade trade) => dbContext.Trades.Add(trade);
}
