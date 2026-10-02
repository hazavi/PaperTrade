using PaperTrade.Domain.Assets;

namespace PaperTrade.UnitTests.Assets;

public sealed class AssetTests
{
    [Fact]
    public void Constructor_NormalizesSymbolAndCurrency()
    {
        var asset = new Asset(
            Guid.NewGuid(),
            " aapl ",
            "Apple Inc.",
            "NASDAQ",
            AssetType.Stock,
            " usd ",
            true);

        Assert.Equal("AAPL", asset.Symbol);
        Assert.Equal("USD", asset.Currency);
    }
}
