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
}
