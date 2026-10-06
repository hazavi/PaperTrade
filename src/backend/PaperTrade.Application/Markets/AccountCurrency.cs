using PaperTrade.Domain.Instruments;

namespace PaperTrade.Application.Markets;

// Portfolios currently hold USD. For USD-base FX, the position represents
// exposure to the pair with its USD notional held as cash collateral.
public static class AccountCurrency
{
    public static decimal NotionalUsd(Instrument instrument, decimal quantity, decimal price) =>
        instrument.AssetClass == AssetClass.Forex && instrument.BaseCurrency == "USD"
            ? quantity : quantity * price;

    public static decimal PnlUsd(Instrument instrument, decimal quantity,
        decimal entryPrice, decimal exitPrice)
    {
        var pnlInQuoteCurrency = (exitPrice - entryPrice) * quantity;
        return instrument.QuoteCurrency == "USD"
            ? pnlInQuoteCurrency
            : pnlInQuoteCurrency / exitPrice;
    }

    public static decimal MarketValueUsd(Instrument instrument, decimal quantity,
        decimal entryPrice, decimal currentPrice) =>
        NotionalUsd(instrument, quantity, entryPrice) +
        PnlUsd(instrument, quantity, entryPrice, currentPrice);
}
