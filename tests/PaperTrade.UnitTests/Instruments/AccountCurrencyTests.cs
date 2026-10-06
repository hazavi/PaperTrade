using PaperTrade.Application.Markets;
using PaperTrade.Domain.Instruments;

namespace PaperTrade.UnitTests.Instruments;

public sealed class AccountCurrencyTests
{
    [Fact]
    public void EuroDollar_ValuesDirectlyInUsd()
    {
        var pair = SupportedPairs.Create("EUR/USD")!;
        Assert.Equal(1100m, AccountCurrency.NotionalUsd(pair, 1000m, 1.10m));
        Assert.Equal(100m, AccountCurrency.PnlUsd(pair, 1000m, 1.10m, 1.20m));
        Assert.Equal(1200m, AccountCurrency.MarketValueUsd(pair, 1000m, 1.10m, 1.20m));
    }

    [Fact]
    public void DollarYen_ConvertsYenPnlAtExitRate()
    {
        var pair = SupportedPairs.Create("USD/JPY")!;
        Assert.Equal(1000m, AccountCurrency.NotionalUsd(pair, 1000m, 150m));
        Assert.Equal(90.91m, decimal.Round(AccountCurrency.PnlUsd(pair, 1000m, 150m, 165m), 2));
        Assert.Equal(1090.91m, decimal.Round(AccountCurrency.MarketValueUsd(pair, 1000m, 150m, 165m), 2));
    }
}
