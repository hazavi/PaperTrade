using Microsoft.Extensions.Logging;
using PaperTrade.Application.Abstractions.Caching;
using PaperTrade.Application.Markets;

namespace PaperTrade.Infrastructure.Markets;

public sealed class CachedMarketDataService(
    FinnhubMarketDataService innerService,
    ICacheService cacheService,
    ILogger<CachedMarketDataService> logger)
    : IMarketDataService
{
    public Task<IReadOnlyList<AssetSummary>> SearchAssetsAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var normalizedQuery = query.Trim().ToLowerInvariant();

        return GetOrCreateAsync(
            $"market:search:{normalizedQuery}",
            TimeSpan.FromMinutes(10),
            token => innerService.SearchAssetsAsync(query, token),
            cancellationToken);
    }

    public Task<MarketQuote?> GetQuoteAsync(
        string symbol,
        CancellationToken cancellationToken)
    {
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();

        return GetOrCreateAsync(
            $"market:quote:{normalizedSymbol}",
            TimeSpan.FromSeconds(15),
            token => innerService.GetQuoteAsync(
                normalizedSymbol,
                token),
            cancellationToken);
    }

    public Task<IReadOnlyList<HistoricalPrice>>
        GetHistoricalPricesAsync(
            string symbol,
            DateTimeOffset from,
            DateTimeOffset to,
            string resolution,
            CancellationToken cancellationToken)
    {
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        var key =
            $"market:history:{normalizedSymbol}:" +
            $"{resolution}:{from.ToUnixTimeSeconds()}:" +
            to.ToUnixTimeSeconds();

        return GetOrCreateAsync(
            key,
            TimeSpan.FromHours(1),
            token => innerService.GetHistoricalPricesAsync(
                normalizedSymbol,
                from,
                to,
                resolution,
                token),
            cancellationToken);
    }

    public Task<MarketStatus> GetMarketStatusAsync(
        string exchange,
        CancellationToken cancellationToken)
    {
        var normalizedExchange = exchange.Trim().ToUpperInvariant();

        return GetOrCreateAsync(
            $"market:status:{normalizedExchange}",
            TimeSpan.FromMinutes(1),
            token => innerService.GetMarketStatusAsync(
                normalizedExchange,
                token),
            cancellationToken);
    }

    private async Task<T> GetOrCreateAsync<T>(
        string key,
        TimeSpan timeToLive,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken)
    {
        try
        {
            var cachedValue = await cacheService.GetAsync<T>(
                key,
                cancellationToken);

            if (cachedValue is not null)
            {
                return cachedValue;
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Cache read failed for {CacheKey}",
                key);
        }

        var value = await factory(cancellationToken);

        try
        {
            await cacheService.SetAsync(
                key,
                value,
                timeToLive,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Cache write failed for {CacheKey}",
                key);
        }

        return value;
    }
}
