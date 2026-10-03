using PaperTrade.Domain.Alerts;

namespace PaperTrade.UnitTests.Engagement;

public sealed class PriceAlertTests
{
    [Theory]
    [InlineData(PriceAlertDirection.Above, 200, 200, true)]
    [InlineData(PriceAlertDirection.Above, 200, 199, false)]
    [InlineData(PriceAlertDirection.Below, 100, 99, true)]
    [InlineData(PriceAlertDirection.Below, 100, 101, false)]
    public void ShouldTrigger_UsesConfiguredThreshold(
        PriceAlertDirection direction, decimal target,
        decimal current, bool expected)
    {
        var alert = new PriceAlert(Guid.NewGuid(), Guid.NewGuid(),
            "aapl", direction, target, DateTimeOffset.UtcNow);

        Assert.Equal(expected, alert.ShouldTrigger(current));
    }

    [Fact]
    public void Trigger_DeactivatesAlert()
    {
        var now = DateTimeOffset.UtcNow;
        var alert = new PriceAlert(Guid.NewGuid(), Guid.NewGuid(),
            "AAPL", PriceAlertDirection.Above, 200, now.AddDays(-1));

        alert.Trigger(now);

        Assert.False(alert.IsActive);
        Assert.Equal(now, alert.TriggeredAt);
        Assert.False(alert.ShouldTrigger(250));
    }
}
