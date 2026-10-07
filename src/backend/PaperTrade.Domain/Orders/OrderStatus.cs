namespace PaperTrade.Domain.Orders;

public enum OrderStatus
{
    Pending,
    PartiallyFilled,
    Filled,
    Rejected,
    Cancelled,
    Expired
}
