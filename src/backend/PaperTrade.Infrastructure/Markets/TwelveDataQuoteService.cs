using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PaperTrade.Application.Markets;
using PaperTrade.Domain.Instruments;

namespace PaperTrade.Infrastructure.Markets;

public sealed class TwelveDataQuoteService(HttpClient client, IOptions<TwelveDataOptions> options)
{
    private readonly TwelveDataOptions _options = options.Value;

    public async Task<MarketQuote?> GetQuoteAsync(Instrument instrument, string providerSymbol,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new MarketDataUnavailableException("Twelve Data is required for FX and metals quotes.");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"quote?symbol={Uri.EscapeDataString(providerSymbol)}&interval=1min&timezone=UTC&dp={instrument.PricePrecision}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("apikey", _options.ApiKey);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<QuoteResponse>(cancellationToken: cancellationToken);
            if (data?.Close is null || data.Close <= 0)
                throw new MarketDataUnavailableException(data?.Message ?? "The quote provider returned no price.");
            if (data.Timestamp is not > 0)
                throw new MarketDataUnavailableException("The quote provider returned no price timestamp. Trading is unavailable until a timestamped quote is received.");

            var mid = data.Close.Value;
            // Twelve Data's composite currency feed supplies a midpoint. The two-sided
            // paper quote is explicitly simulated at one pip (two for metals).
            var spread = instrument.AssetClass == AssetClass.Metal
                ? 2 * instrument.TickSize : instrument.PipSize;
            var bid = decimal.Round((mid - spread / 2) / instrument.TickSize, 0) * instrument.TickSize;
            var ask = bid + spread;
            return new MarketQuote(instrument.Symbol, mid,
                data.Change ?? 0, data.PercentChange ?? 0,
                data.Open ?? mid, data.High ?? mid, data.Low ?? mid,
                data.PreviousClose ?? mid,
                DateTimeOffset.FromUnixTimeSeconds(data.Timestamp.Value),
                bid, ask, spread, true, "Twelve Data");
        }
        catch (MarketDataUnavailableException) { throw; }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            throw new MarketDataUnavailableException("The Twelve Data quote provider is unavailable.", exception);
        }
    }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    private sealed record QuoteResponse(
        [property: JsonPropertyName("close")] decimal? Close,
        [property: JsonPropertyName("change")] decimal? Change,
        [property: JsonPropertyName("percent_change")] decimal? PercentChange,
        [property: JsonPropertyName("open")] decimal? Open,
        [property: JsonPropertyName("high")] decimal? High,
        [property: JsonPropertyName("low")] decimal? Low,
        [property: JsonPropertyName("previous_close")] decimal? PreviousClose,
        [property: JsonPropertyName("timestamp")] long? Timestamp,
        [property: JsonPropertyName("message")] string? Message);
}
