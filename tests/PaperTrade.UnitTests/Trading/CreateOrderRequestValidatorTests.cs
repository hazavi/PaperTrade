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
