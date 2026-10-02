using PaperTrade.Domain.Assets;

namespace PaperTrade.Application.Markets;

public sealed record AssetSummary(
    string Symbol,
    string Name,
    string Exchange,
    AssetType Type,
    string Currency);

public sealed record MarketQuote(
    string Symbol,
    decimal CurrentPrice,
    decimal Change,
    decimal PercentChange,
    decimal Open,
    decimal High,
    decimal Low,
    decimal PreviousClose,
    DateTimeOffset Timestamp);

public sealed record HistoricalPrice(
    DateTimeOffset Time,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal Volume);

public sealed record MarketStatus(
    string Exchange,
    bool IsOpen,
    string Session,
    string Timezone,
    DateTimeOffset Timestamp);
