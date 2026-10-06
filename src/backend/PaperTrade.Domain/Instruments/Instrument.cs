namespace PaperTrade.Domain.Instruments;

public sealed class Instrument
{
    private Instrument() { }

    public Instrument(Guid id, string symbol, string displayName,
        AssetClass assetClass, string exchange, string? baseCurrency,
        string quoteCurrency, int pricePrecision, int quantityPrecision,
        decimal tickSize, decimal minimumOrderSize, string marketTimeZone,
        string tradingSession, bool isTradable)
    {
        if (id == Guid.Empty) throw new ArgumentException("Instrument ID cannot be empty.", nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(exchange);
        ArgumentException.ThrowIfNullOrWhiteSpace(quoteCurrency);
        ArgumentException.ThrowIfNullOrWhiteSpace(marketTimeZone);
        ArgumentException.ThrowIfNullOrWhiteSpace(tradingSession);
        if (!Enum.IsDefined(assetClass)) throw new ArgumentOutOfRangeException(nameof(assetClass));
        if (pricePrecision is < 0 or > 6) throw new ArgumentOutOfRangeException(nameof(pricePrecision));
        if (quantityPrecision is < 0 or > 6) throw new ArgumentOutOfRangeException(nameof(quantityPrecision));
        if (tickSize <= 0) throw new ArgumentOutOfRangeException(nameof(tickSize));
        if (minimumOrderSize <= 0) throw new ArgumentOutOfRangeException(nameof(minimumOrderSize));
        if (decimal.Round(tickSize, pricePrecision) != tickSize)
            throw new ArgumentException("Tick size exceeds price precision.", nameof(tickSize));
        if (decimal.Round(minimumOrderSize, quantityPrecision) != minimumOrderSize)
            throw new ArgumentException("Minimum order size exceeds quantity precision.", nameof(minimumOrderSize));

        Id = id;
        Symbol = symbol.Trim().ToUpperInvariant();
        DisplayName = displayName.Trim();
        AssetClass = assetClass;
        Exchange = exchange.Trim().ToUpperInvariant();
        BaseCurrency = string.IsNullOrWhiteSpace(baseCurrency) ? null : baseCurrency.Trim().ToUpperInvariant();
        QuoteCurrency = quoteCurrency.Trim().ToUpperInvariant();
        PricePrecision = pricePrecision;
        QuantityPrecision = quantityPrecision;
        TickSize = tickSize;
        MinimumOrderSize = minimumOrderSize;
        MarketTimeZone = marketTimeZone.Trim();
        TradingSession = tradingSession.Trim();
        IsTradable = isTradable;
    }

    public Guid Id { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public AssetClass AssetClass { get; private set; }
    public string Exchange { get; private set; } = string.Empty;
    public string? BaseCurrency { get; private set; }
    public string QuoteCurrency { get; private set; } = string.Empty;
    public int PricePrecision { get; private set; }
    public int QuantityPrecision { get; private set; }
    public decimal TickSize { get; private set; }
    public decimal MinimumOrderSize { get; private set; }
    public string MarketTimeZone { get; private set; } = string.Empty;
    public string TradingSession { get; private set; } = string.Empty;
    public bool IsTradable { get; private set; }

    public void UpdateMetadata(string displayName, string exchange)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(exchange);
        DisplayName = displayName.Trim();
        Exchange = exchange.Trim().ToUpperInvariant();
    }

    public static Instrument UsEquity(string symbol, string? name = null,
        AssetClass assetClass = AssetClass.Equity, string exchange = "US") =>
        new(Guid.NewGuid(), symbol, string.IsNullOrWhiteSpace(name) ? symbol.Trim().ToUpperInvariant() : name,
            assetClass, exchange, null, "USD", 2, 6, 0.01m, 0.000001m,
            "America/New_York", "US equities", true);
}
