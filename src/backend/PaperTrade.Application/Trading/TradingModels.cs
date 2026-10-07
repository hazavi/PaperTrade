using PaperTrade.Application.Markets;

namespace PaperTrade.Application.Trading;

public sealed record CreateOrderRequest(
    string Symbol,
    string Side,
    string Type,
    decimal Quantity,
    decimal? Price = null,
    decimal? TakeProfit = null,
    decimal? StopLoss = null,
    DateTimeOffset? ExpiresAt = null);

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
    DateTimeOffset? ExecutedAt,
    Guid InstrumentId = default,
    InstrumentDto? Instrument = null,
    decimal FilledQuantity = 0,
    Guid? ParentOrderId = null,
    DateTimeOffset? ExpiresAt = null,
    DateTimeOffset? ClosedAt = null);

public enum OrderExecutionStatus
{
    Filled,
    Pending,
    PortfolioNotFound,
    QuoteNotFound,
    StaleQuote,
    InsufficientFunds,
    InsufficientQuantity,
    InvalidOrder,
    RiskLimitExceeded
}

public sealed record OrderExecutionResult(
    OrderExecutionStatus Status,
    OrderDto? Order,
    decimal? CashBalance,
    decimal? OwnedQuantity);

public sealed record ExecutionDto(Guid Id, Guid OrderId, string Symbol,
    string Side, decimal Quantity, decimal QuotePrice, decimal Price,
    decimal TotalValue, decimal Fee, decimal RealizedPnl, DateTimeOffset ExecutedAt);
