namespace PaperTrade.Domain.Instruments;

public static class SupportedPairs
{
    public static readonly IReadOnlyList<string> Symbols =
    [
        "EUR/USD", "GBP/USD", "USD/JPY", "USD/CHF", "AUD/USD",
        "USD/CAD", "NZD/USD", "XAU/USD", "XAG/USD"
    ];

    public static Instrument? Create(string symbol)
    {
        var normalized = symbol.Trim().ToUpperInvariant();
        if (!Symbols.Contains(normalized)) return null;
        var parts = normalized.Split('/');
        return parts[0] is "XAU" or "XAG"
            ? Instrument.SpotMetal(parts[0])
            : Instrument.CurrencyPair(parts[0], parts[1]);
    }
}
