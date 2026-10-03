using PaperTrade.Domain.Positions;

namespace PaperTrade.UnitTests.Trading;

public sealed class PositionTests
{
    [Fact]
    public void Add_RecalculatesWeightedAverageEntryPrice()
    {
        var position = CreatePosition(5, 100);

        position.Add(5, 120, DateTimeOffset.UtcNow);

        Assert.Equal(10, position.Quantity);
        Assert.Equal(110, position.AverageEntryPrice);
    }

    [Fact]
    public void Sell_ReturnsRealizedPnlAndReducesQuantity()
    {
        var position = CreatePosition(10, 100);

        var pnl = position.Sell(5, 120, DateTimeOffset.UtcNow);

        Assert.Equal(100, pnl);
        Assert.Equal(5, position.Quantity);
        Assert.Equal(100, position.AverageEntryPrice);
    }

    [Fact]
    public void Sell_MoreThanOwned_IsRejected()
    {
        var position = CreatePosition(2, 100);

        Assert.Throws<InvalidOperationException>(() =>
            position.Sell(3, 100, DateTimeOffset.UtcNow));
    }

    private static Position CreatePosition(decimal quantity, decimal price) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "aapl", quantity, price,
            DateTimeOffset.UtcNow);
}
