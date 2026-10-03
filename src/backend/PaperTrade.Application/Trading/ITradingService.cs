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
}
