using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PaperTrade.Application.Markets;
using PaperTrade.Domain.Assets;

namespace PaperTrade.Infrastructure.Markets;

public sealed class FinnhubMarketDataService(
    HttpClient httpClient,
    IOptions<FinnhubOptions> options)
    : IMarketDataService
{
    private readonly FinnhubOptions _options = options.Value;

    public async Task<IReadOnlyList<AssetSummary>> SearchAssetsAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var response = await GetAsync<FinnhubSearchResponse>(
            $"search?q={Uri.EscapeDataString(query.Trim())}",
            cancellationToken);

        return response.Result
            .Where(result =>
                !string.IsNullOrWhiteSpace(result.Symbol) &&
                !result.Symbol.Contains(':'))
            .Take(20)
            .Select(result => new AssetSummary(
                result.Symbol.Trim().ToUpperInvariant(),
                string.IsNullOrWhiteSpace(result.Description)
                    ? result.Symbol
                    : result.Description.Trim(),
                "US",
                MapAssetType(result.Type),
                "USD"))
            .ToArray();
    }

    public async Task<MarketQuote?> GetQuoteAsync(
        string symbol,
        CancellationToken cancellationToken)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        var response = await GetAsync<FinnhubQuoteResponse>(
            $"quote?symbol={Uri.EscapeDataString(normalizedSymbol)}",
            cancellationToken);

        if (response.Timestamp <= 0 || response.CurrentPrice <= 0)
        {
            return null;
        }

        return new MarketQuote(
            normalizedSymbol,
            response.CurrentPrice,
            response.Change,
            response.PercentChange,
            response.Open,
            response.High,
            response.Low,
            response.PreviousClose,
            DateTimeOffset.FromUnixTimeSeconds(response.Timestamp));
    }

    public async Task<IReadOnlyList<HistoricalPrice>>
        GetHistoricalPricesAsync(
            string symbol,
            DateTimeOffset from,
            DateTimeOffset to,
            string resolution,
            CancellationToken cancellationToken)
    {
        var normalizedSymbol = NormalizeSymbol(symbol);
        var path =
            "stock/candle" +
            $"?symbol={Uri.EscapeDataString(normalizedSymbol)}" +
            $"&resolution={Uri.EscapeDataString(resolution)}" +
            $"&from={from.ToUnixTimeSeconds()}" +
            $"&to={to.ToUnixTimeSeconds()}";

        FinnhubCandleResponse response;
        try
        {
            response = await GetAsync<FinnhubCandleResponse>(
                path,
                cancellationToken);
        }
        catch (MarketDataUnavailableException exception)
            when (exception.InnerException is HttpRequestException
            {
                StatusCode: HttpStatusCode.Forbidden
            })
        {
            // Finnhub restricts candle data on some plans. An empty series is
            // a supported result so quotes and trading can continue to work.
            return [];
        }

        if (!string.Equals(
                response.Status,
                "ok",
                StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        var count = new[]
        {
            response.Timestamps.Count,
            response.Open.Count,
            response.High.Count,
            response.Low.Count,
            response.Close.Count,
            response.Volume.Count
        }.Min();

        return Enumerable.Range(0, count)
            .Select(index => new HistoricalPrice(
                DateTimeOffset.FromUnixTimeSeconds(
                    response.Timestamps[index]),
                response.Open[index],
                response.High[index],
                response.Low[index],
                response.Close[index],
                response.Volume[index]))
            .ToArray();
    }

    public async Task<MarketStatus> GetMarketStatusAsync(
        string exchange,
        CancellationToken cancellationToken)
    {
        var normalizedExchange = exchange.Trim().ToUpperInvariant();
        var response = await GetAsync<FinnhubMarketStatusResponse>(
            "stock/market-status" +
            $"?exchange={Uri.EscapeDataString(normalizedExchange)}",
            cancellationToken);

        return new MarketStatus(
            normalizedExchange,
            response.IsOpen,
            response.Session ?? "unknown",
            response.Timezone ?? "unknown",
            response.Timestamp > 0
                ? DateTimeOffset.FromUnixTimeSeconds(response.Timestamp)
                : DateTimeOffset.UtcNow);
    }

    private async Task<T> GetAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new MarketDataUnavailableException(
                "Market data is not configured.");
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                path);

            request.Headers.Add("X-Finnhub-Token", _options.ApiKey);

            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>(
                       cancellationToken: cancellationToken)
                   ?? throw new MarketDataUnavailableException(
                       "The market-data provider returned an empty response.");
        }
        catch (MarketDataUnavailableException)
        {
            throw;
        }
        catch (Exception exception)
            when (exception is HttpRequestException or
                  TaskCanceledException or
                  System.Text.Json.JsonException)
        {
            throw new MarketDataUnavailableException(
                "The market-data provider is unavailable.",
                exception);
        }
    }

    private static string NormalizeSymbol(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        return symbol.Trim().ToUpperInvariant();
    }

    private static AssetType MapAssetType(string? type)
    {
        return type?.Contains(
            "ETF",
            StringComparison.OrdinalIgnoreCase) == true
                ? AssetType.ExchangeTradedFund
                : AssetType.Stock;
    }

    private sealed record FinnhubSearchResponse(
        [property: JsonPropertyName("result")]
        IReadOnlyList<FinnhubSearchResult> Result);

    private sealed record FinnhubSearchResult(
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("symbol")] string Symbol,
        [property: JsonPropertyName("type")] string? Type);

    private sealed record FinnhubQuoteResponse(
        [property: JsonPropertyName("c")] decimal CurrentPrice,
        [property: JsonPropertyName("d")] decimal Change,
        [property: JsonPropertyName("dp")] decimal PercentChange,
        [property: JsonPropertyName("h")] decimal High,
        [property: JsonPropertyName("l")] decimal Low,
        [property: JsonPropertyName("o")] decimal Open,
        [property: JsonPropertyName("pc")] decimal PreviousClose,
        [property: JsonPropertyName("t")] long Timestamp);

    private sealed record FinnhubCandleResponse(
        [property: JsonPropertyName("c")]
        IReadOnlyList<decimal> Close,
        [property: JsonPropertyName("h")]
        IReadOnlyList<decimal> High,
        [property: JsonPropertyName("l")]
        IReadOnlyList<decimal> Low,
        [property: JsonPropertyName("o")]
        IReadOnlyList<decimal> Open,
        [property: JsonPropertyName("s")] string Status,
        [property: JsonPropertyName("t")]
        IReadOnlyList<long> Timestamps,
        [property: JsonPropertyName("v")]
        IReadOnlyList<decimal> Volume);

    private sealed record FinnhubMarketStatusResponse(
        [property: JsonPropertyName("isOpen")] bool IsOpen,
        [property: JsonPropertyName("session")] string? Session,
        [property: JsonPropertyName("timezone")] string? Timezone,
        [property: JsonPropertyName("t")] long Timestamp);
}
