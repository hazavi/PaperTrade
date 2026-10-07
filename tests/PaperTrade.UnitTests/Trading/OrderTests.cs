using PaperTrade.Domain.Orders;

namespace PaperTrade.UnitTests.Trading;

public sealed class OrderTests
{
    [Fact]
    public void Fill_RecordsExecutionAndStatus()
    {
        var executedAt = DateTimeOffset.UtcNow;
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "aapl",
            OrderSide.Buy, OrderType.Market, 5, 100, executedAt.AddSeconds(-1));

        order.Fill(101.25m, executedAt);

        Assert.Equal(OrderStatus.Filled, order.Status);
        Assert.Equal(101.25m, order.ExecutedPrice);
        Assert.Equal(executedAt, order.ExecutedAt);
        Assert.Equal("AAPL", order.Symbol);
    }

    [Fact]
    public void FilledOrder_CannotBeFilledTwice()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "AAPL",
            OrderSide.Buy, OrderType.Market, 1, 100, DateTimeOffset.UtcNow);
        order.Fill(100, DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            order.Fill(100, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void PartialFills_AccumulateQuantityAndWeightedPrice()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "AAPL",
            OrderSide.Buy, OrderType.Limit, 10, 100, DateTimeOffset.UtcNow);
        order.Fill(99, DateTimeOffset.UtcNow, 4);
        Assert.Equal(OrderStatus.PartiallyFilled, order.Status);
        Assert.Equal(6, order.RemainingQuantity);
        order.Fill(100, DateTimeOffset.UtcNow, 6);
        Assert.Equal(OrderStatus.Filled, order.Status);
        Assert.Equal(99.6m, order.ExecutedPrice);
        Assert.Equal(10, order.FilledQuantity);
    }

    [Fact]
    public void CancelledOrder_CannotFill()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "AAPL",
            OrderSide.Buy, OrderType.Stop, 1, 100, DateTimeOffset.UtcNow);
        order.Cancel(DateTimeOffset.UtcNow);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Throws<InvalidOperationException>(() => order.Fill(100, DateTimeOffset.UtcNow));
    }
}
