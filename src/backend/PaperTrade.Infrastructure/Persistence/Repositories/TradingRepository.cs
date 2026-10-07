using Microsoft.EntityFrameworkCore;
using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Domain.Orders;
using PaperTrade.Domain.Positions;
using PaperTrade.Domain.Trades;

namespace PaperTrade.Infrastructure.Persistence.Repositories;

internal sealed class TradingRepository(PaperTradeDbContext dbContext)
    : ITradingRepository
{
    public Task<Position?> GetPositionAsync(Guid portfolioId, Guid instrumentId, CancellationToken cancellationToken)
    {
        return dbContext.Positions.SingleOrDefaultAsync(
            position => position.PortfolioId == portfolioId && position.InstrumentId == instrumentId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Position>> GetPositionsAsync(Guid portfolioId, CancellationToken cancellationToken)
    {
        return await dbContext.Positions.AsNoTracking()
            .Include(position => position.Instrument)
            .Where(position => position.PortfolioId == portfolioId)
            .OrderBy(position => position.Symbol)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetOrdersAsync(Guid portfolioId, CancellationToken cancellationToken)
    {
        return await dbContext.Orders.AsNoTracking()
            .Include(order => order.Instrument)
            .Where(order => order.PortfolioId == portfolioId)
            .OrderByDescending(order => order.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        dbContext.Orders.Include(order => order.Instrument)
            .SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetOpenOrdersAsync(CancellationToken cancellationToken) =>
        await dbContext.Orders.AsNoTracking()
            .Where(order => order.Status == OrderStatus.Pending || order.Status == OrderStatus.PartiallyFilled)
            .OrderBy(order => order.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Order>> GetChildOrdersAsync(Guid parentOrderId, CancellationToken cancellationToken) =>
        await dbContext.Orders.Where(order => order.ParentOrderId == parentOrderId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Trade>> GetExecutionsAsync(Guid portfolioId, CancellationToken cancellationToken) =>
        await dbContext.Trades.AsNoTracking().Where(trade => trade.PortfolioId == portfolioId)
            .OrderByDescending(trade => trade.ExecutedAt).ToListAsync(cancellationToken);

    public void AddPosition(Position position) => dbContext.Positions.Add(position);
    public void RemovePosition(Position position) => dbContext.Positions.Remove(position);
    public void AddOrder(Order order) => dbContext.Orders.Add(order);
    public void AddTrade(Trade trade) => dbContext.Trades.Add(trade);
}
