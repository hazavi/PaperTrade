using Microsoft.Extensions.Logging;
using PaperTrade.Application.Abstractions.Caching;
using PaperTrade.Application.Markets;
using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Domain.Instruments;

namespace PaperTrade.Infrastructure.Markets;

public sealed class CachedMarketDataService(
    FinnhubMarketDataService innerService,
    TwelveDataHistoryService twelveDataHistoryService,
    ICacheService cacheService,
    ILogger<CachedMarketDataService> logger,
    IInstrumentRepository? instrumentRepository = null,
    TwelveDataQuoteService? twelveDataQuoteService = null,
    ExpandedMarketAccess? expandedAccess = null)
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
        if (SupportedPairs.IsExpanded(normalizedSymbol) && expandedAccess?.Available != true)
            throw new ExpandedMarketAccessException();

        return GetOrCreateAsync(
            $"market:quote:{normalizedSymbol}",
            TimeSpan.FromSeconds(5),
            async token =>
            {
                var pair = SupportedPairs.Create(normalizedSymbol);
                if (pair is not null)
                {
                    if (SupportedPairs.IsSimulatedIndex(normalizedSymbol))
                        return SimulatedIndexData.Quote(normalizedSymbol, DateTimeOffset.UtcNow);
                    if (SupportedPairs.IsExpanded(normalizedSymbol) && expandedAccess?.Available != true)
                        throw new ExpandedMarketAccessException();
                    if (twelveDataQuoteService is null)
                        throw new MarketDataUnavailableException("FX and metals quotes are not configured.");
                    var instrument = instrumentRepository is null ? pair :
                        await instrumentRepository.UpsertPairAsync(pair, token);
                    var twelveSymbol = await ResolveAsync(normalizedSymbol, "twelvedata", token);
                    return await twelveDataQuoteService.GetQuoteAsync(instrument, twelveSymbol, token);
                }
                var providerSymbol = await ResolveAsync(normalizedSymbol, "finnhub", token);
                var quote = await innerService.GetQuoteAsync(providerSymbol, token);
                return quote is null ? null : quote with { Symbol = normalizedSymbol, Source = "Finnhub" };
            },
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
        if (SupportedPairs.IsExpanded(normalizedSymbol) && expandedAccess?.Available != true)
            throw new ExpandedMarketAccessException();
        var key =
            $"market:history:v2:{normalizedSymbol}:" +
            $"{resolution}:{from.ToUnixTimeSeconds()}:" +
            to.ToUnixTimeSeconds();

        var timeToLive = resolution is "D" or "W"
            ? TimeSpan.FromMinutes(15)
            : TimeSpan.FromMinutes(1);

        return GetOrCreateAsync(
            key,
            timeToLive,
            async token =>
            {
                if (SupportedPairs.IsSimulatedIndex(normalizedSymbol))
                    return SimulatedIndexData.History(normalizedSymbol, from, to, resolution);
                if (SupportedPairs.IsExpanded(normalizedSymbol) && expandedAccess?.Available != true)
                    throw new ExpandedMarketAccessException();
                if (twelveDataHistoryService.IsConfigured)
                {
                    try
                    {
                        var providerSymbol = await ResolveAsync(normalizedSymbol, "twelvedata", token);
                        var prices = await twelveDataHistoryService
                            .GetHistoricalPricesAsync(
                                providerSymbol, from, to, resolution, token);
                        if (prices.Count > 0)
                        {
                            return prices.Select(p => p with { Source = "Twelve Data" }).ToArray();
                        }
                    }
                    catch (MarketDataUnavailableException exception)
                    {
                        logger.LogWarning(exception,
                            "Twelve Data history failed for {Symbol}; falling back to Finnhub",
                            normalizedSymbol);
                    }
                }

                if (SupportedPairs.Create(normalizedSymbol) is not null) return [];
                var finnhubSymbol = await ResolveAsync(normalizedSymbol, "finnhub", token);
                return (await innerService.GetHistoricalPricesAsync(
                    finnhubSymbol, from, to, resolution, token))
                    .Select(p => p with { Source = "Finnhub" }).ToArray();
            },
            cancellationToken);
    }

    private async Task<string> ResolveAsync(string symbol, string provider, CancellationToken token) =>
        instrumentRepository is null
            ? symbol
            : await instrumentRepository.GetProviderSymbolAsync(symbol, provider, token) ?? symbol;

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
