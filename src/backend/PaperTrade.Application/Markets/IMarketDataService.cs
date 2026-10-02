namespace PaperTrade.Application.Markets;

public interface IMarketDataService
{
    Task<IReadOnlyList<AssetSummary>> SearchAssetsAsync(
        string query,
        CancellationToken cancellationToken);

    Task<MarketQuote?> GetQuoteAsync(
        string symbol,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<HistoricalPrice>> GetHistoricalPricesAsync(
        string symbol,
        DateTimeOffset from,
        DateTimeOffset to,
        string resolution,
        CancellationToken cancellationToken);

    Task<MarketStatus> GetMarketStatusAsync(
        string exchange,
        CancellationToken cancellationToken);
}
