namespace PaperTrade.Domain.Instruments;

public sealed class ProviderSymbol
{
    private ProviderSymbol() { }

    public ProviderSymbol(Guid instrumentId, string provider, string symbol)
    {
        if (instrumentId == Guid.Empty) throw new ArgumentException("Instrument ID cannot be empty.", nameof(instrumentId));
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        InstrumentId = instrumentId;
        Provider = provider.Trim().ToLowerInvariant();
        Symbol = symbol.Trim().ToUpperInvariant();
    }

    public Guid InstrumentId { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string Symbol { get; private set; } = string.Empty;
    public Instrument Instrument { get; private set; } = null!;
}
