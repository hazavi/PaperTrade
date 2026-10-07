namespace PaperTrade.Application.Trading;

public interface ITradingService
{
    Task<OrderExecutionResult> PlaceOrderAsync(
        Guid userId,
        CreateOrderRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderDto>> GetOrdersAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ExecutionDto>> GetExecutionsAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> CancelOrderAsync(Guid userId, Guid orderId, CancellationToken cancellationToken);
    Task ProcessPendingOrdersAsync(CancellationToken cancellationToken);
    Task ProcessMarginAsync(CancellationToken cancellationToken);
}
