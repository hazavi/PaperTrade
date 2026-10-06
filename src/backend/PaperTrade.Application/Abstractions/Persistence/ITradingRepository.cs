using PaperTrade.Domain.Orders;
using PaperTrade.Domain.Positions;
using PaperTrade.Domain.Trades;

namespace PaperTrade.Application.Abstractions.Persistence;

public interface ITradingRepository
{
    Task<Position?> GetPositionAsync(Guid portfolioId, Guid instrumentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Position>> GetPositionsAsync(Guid portfolioId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> GetOrdersAsync(Guid portfolioId, CancellationToken cancellationToken);
    void AddPosition(Position position);
    void RemovePosition(Position position);
    void AddOrder(Order order);
    void AddTrade(Trade trade);
}
