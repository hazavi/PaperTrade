using PaperTrade.Domain.Instruments;

namespace PaperTrade.Application.Markets;

public sealed record InstrumentDto(
    Guid Id, string Symbol, string DisplayName, string AssetClass,
    string Exchange, string? BaseCurrency, string QuoteCurrency,
    int PricePrecision, int QuantityPrecision, decimal TickSize,
    decimal MinimumOrderSize, string MarketTimeZone, string TradingSession,
    bool IsTradable, decimal PipSize = 0, decimal LotSize = 1)
{
    public static InstrumentDto From(Instrument instrument) => new(
        instrument.Id, instrument.Symbol, instrument.DisplayName,
        instrument.AssetClass.ToString().ToLowerInvariant(),
        instrument.Exchange, instrument.BaseCurrency, instrument.QuoteCurrency,
        instrument.PricePrecision, instrument.QuantityPrecision, instrument.TickSize,
        instrument.MinimumOrderSize, instrument.MarketTimeZone, instrument.TradingSession,
        instrument.IsTradable, instrument.PipSize, instrument.LotSize);
}
