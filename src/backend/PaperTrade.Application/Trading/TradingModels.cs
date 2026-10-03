namespace PaperTrade.Application.Trading;

public sealed record CreateOrderRequest(
    string Symbol,
    string Side,
    string Type,
    decimal Quantity);

public sealed record OrderDto(
    Guid Id,
    Guid PortfolioId,
    string Symbol,
    string Side,
    string Type,
    decimal Quantity,
    decimal RequestedPrice,
    decimal? ExecutedPrice,
    decimal? TotalValue,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExecutedAt);

public enum OrderExecutionStatus
{
    Filled,
    PortfolioNotFound,
    QuoteNotFound,
    InsufficientFunds,
    InsufficientQuantity,
    InvalidOrder
}

public sealed record OrderExecutionResult(
    OrderExecutionStatus Status,
    OrderDto? Order,
    decimal? CashBalance,
    decimal? OwnedQuantity);
