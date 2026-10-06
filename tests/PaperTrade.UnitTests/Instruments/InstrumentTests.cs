using PaperTrade.Domain.Instruments;

namespace PaperTrade.UnitTests.Instruments;

public sealed class InstrumentTests
{
    [Fact]
    public void Constructor_NormalizesCanonicalFields()
    {
        var instrument = new Instrument(Guid.NewGuid(), " xau/usd ", " Gold ",
            AssetClass.Metal, " otc ", " xau ", " usd ", 3, 2,
            0.005m, 0.01m, "UTC", "weekdays", false);

        Assert.Equal("XAU/USD", instrument.Symbol);
        Assert.Equal("Gold", instrument.DisplayName);
        Assert.Equal("XAU", instrument.BaseCurrency);
        Assert.Equal("USD", instrument.QuoteCurrency);
        Assert.False(instrument.IsTradable);
    }

    [Fact]
    public void Constructor_RejectsTickSizeBeyondPricePrecision()
    {
        Assert.Throws<ArgumentException>(() => new Instrument(Guid.NewGuid(), "TEST",
            "Test", AssetClass.Equity, "US", null, "USD", 2, 6,
            0.001m, 0.000001m, "America/New_York", "US equities", true));
    }

    [Fact]
    public void UsEquity_UsesCanonicalSymbolAsPlaceholderName()
    {
        var instrument = Instrument.UsEquity("aapl");
        Assert.Equal("AAPL", instrument.Symbol);
        Assert.Equal("AAPL", instrument.DisplayName);
    }

    [Theory]
    [InlineData("EUR/USD", 5, 0.00001, 0.0001, 1000, 100000)]
    [InlineData("USD/JPY", 3, 0.001, 0.01, 1000, 100000)]
    [InlineData("XAU/USD", 3, 0.001, 0.001, 0.01, 100)]
    [InlineData("XAG/USD", 3, 0.001, 0.001, 0.01, 5000)]
    public void SupportedPair_HasMarketSpecificContract(string symbol, int precision,
        decimal tick, decimal pip, decimal minimum, decimal lot)
    {
        var instrument = SupportedPairs.Create(symbol)!;
        Assert.Equal(precision, instrument.PricePrecision);
        Assert.Equal(tick, instrument.TickSize);
        Assert.Equal(pip, instrument.PipSize);
        Assert.Equal(minimum, instrument.MinimumOrderSize);
        Assert.Equal(lot, instrument.LotSize);
    }
}
