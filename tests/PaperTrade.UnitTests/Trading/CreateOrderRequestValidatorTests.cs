using PaperTrade.Application.Trading;
using PaperTrade.Application.Trading.Validation;

namespace PaperTrade.UnitTests.Trading;

public sealed class CreateOrderRequestValidatorTests
{
    private readonly CreateOrderRequestValidator _validator = new();

    [Fact]
    public async Task ValidMarketBuy_Passes()
    {
        var result = await _validator.ValidateAsync(
            new CreateOrderRequest("AAPL", "buy", "market", 2.5m));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("limit")]
    [InlineData("stop")]
    public async Task PendingPriceOrder_WithPrice_Passes(string type)
    {
        Assert.True((await _validator.ValidateAsync(
            new CreateOrderRequest("EUR/USD", "buy", type, 1000, Price: 1.1m))).IsValid);
    }

    [Fact]
    public async Task BracketRequiresBothExits()
    {
        Assert.False((await _validator.ValidateAsync(
            new CreateOrderRequest("AAPL", "buy", "bracket", 1, TakeProfit: 120))).IsValid);
        Assert.True((await _validator.ValidateAsync(
            new CreateOrderRequest("AAPL", "buy", "bracket", 1,
                TakeProfit: 120, StopLoss: 90))).IsValid);
    }

    [Theory]
    [InlineData("hold", "market", 1)]
    [InlineData("buy", "limit", 1)]
    [InlineData("buy", "market", 0)]
    public async Task UnsupportedOrNonpositiveOrder_Fails(
        string side, string type, decimal quantity)
    {
        var result = await _validator.ValidateAsync(
            new CreateOrderRequest("AAPL", side, type, quantity));

        Assert.False(result.IsValid);
    }
}
