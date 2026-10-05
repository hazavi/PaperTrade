using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PaperTrade.Application.Markets;

namespace PaperTrade.Infrastructure.Markets;

public sealed class TwelveDataHistoryService(
    HttpClient httpClient,
    IOptions<TwelveDataOptions> options)
{
    private readonly TwelveDataOptions _options = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<IReadOnlyList<HistoricalPrice>> GetHistoricalPricesAsync(
        string symbol,
        DateTimeOffset from,
        DateTimeOffset to,
        string resolution,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return [];
        }

        var interval = resolution switch
        {
            "1" => "1min",
            "5" => "5min",
            "15" => "15min",
            "30" => "30min",
            "60" => "1h",
            "D" => "1day",
            "W" => "1week",
            _ => throw new ArgumentOutOfRangeException(
                nameof(resolution), resolution, "Unsupported history resolution.")
        };

        var path = "time_series" +
                   $"?symbol={Uri.EscapeDataString(symbol.Trim().ToUpperInvariant())}" +
                   $"&interval={Uri.EscapeDataString(interval)}" +
                   $"&start_date={Uri.EscapeDataString(from.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))}" +
                   $"&end_date={Uri.EscapeDataString(to.UtcDateTime.ToString("O", CultureInfo.InvariantCulture))}" +
                   "&order=asc&timezone=UTC&adjust=splits&outputsize=5000";

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "apikey", _options.ApiKey);

        try
        {
            using var response = await httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<TwelveDataResponse>(
                cancellationToken: cancellationToken);

            if (payload?.Values is null ||
                !string.Equals(payload.Status, "ok", StringComparison.OrdinalIgnoreCase))
            {
                throw new MarketDataUnavailableException(
                    payload?.Message ?? "The history provider returned no candle data.");
            }

            return payload.Values
                .Select(MapPrice)
                .Where(price => price is not null)
                .Select(price => price!)
                .OrderBy(price => price.Time)
                .ToArray();
        }
        catch (MarketDataUnavailableException)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or
                                           TaskCanceledException or
                                           System.Text.Json.JsonException)
        {
            throw new MarketDataUnavailableException(
                "The Twelve Data history provider is unavailable.", exception);
        }
    }

    private static HistoricalPrice? MapPrice(TwelveDataValue value)
    {
        if (!DateTimeOffset.TryParse(
                value.DateTime,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var timestamp))
        {
            return null;
        }

        return new HistoricalPrice(
            timestamp,
            value.Open,
            value.High,
            value.Low,
            value.Close,
            value.Volume ?? 0);
    }

    private sealed record TwelveDataResponse(
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("values")] IReadOnlyList<TwelveDataValue>? Values);

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    private sealed record TwelveDataValue(
        [property: JsonPropertyName("datetime")] string DateTime,
        [property: JsonPropertyName("open")] decimal Open,
        [property: JsonPropertyName("high")] decimal High,
        [property: JsonPropertyName("low")] decimal Low,
        [property: JsonPropertyName("close")] decimal Close,
        [property: JsonPropertyName("volume")] decimal? Volume);
}
