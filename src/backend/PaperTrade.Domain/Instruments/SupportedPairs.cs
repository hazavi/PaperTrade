namespace PaperTrade.Domain.Instruments;

public static class SupportedPairs
{
    public static readonly IReadOnlyList<string> Symbols =
    [
        "EUR/USD", "GBP/USD", "USD/JPY", "USD/CHF", "AUD/USD",
        "USD/CAD", "NZD/USD", "XAU/USD", "XAG/USD"
    ];
    public static readonly IReadOnlyList<string> ExpandedSymbols =
        ["XPT/USD", "XPD/USD", "BTC/USD", "ETH/USD"];
    public static readonly IReadOnlyList<string> SimulatedIndices =
        ["PT500", "PT100", "PT30"];
    public static bool IsExpanded(string symbol) => ExpandedSymbols.Contains(symbol.Trim().ToUpperInvariant());
    public static bool IsSimulatedIndex(string symbol) => SimulatedIndices.Contains(symbol.Trim().ToUpperInvariant());

    public static Instrument? Create(string symbol)
    {
        var normalized = symbol.Trim().ToUpperInvariant();
        if (IsSimulatedIndex(normalized)) return Instrument.SimulatedIndex(normalized);
        if (!Symbols.Contains(normalized) && !ExpandedSymbols.Contains(normalized)) return null;
        var parts = normalized.Split('/');
        return parts[0] is "XAU" or "XAG"
            ? Instrument.SpotMetal(parts[0])
            : parts[0] is "XPT" or "XPD" ? Instrument.SpotCommodity(parts[0])
            : parts[0] is "BTC" or "ETH" ? Instrument.Cryptocurrency(parts[0])
            : Instrument.CurrencyPair(parts[0], parts[1]);
    }
}
