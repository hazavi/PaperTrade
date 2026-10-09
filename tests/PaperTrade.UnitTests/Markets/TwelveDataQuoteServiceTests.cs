using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using PaperTrade.Domain.Instruments;
using PaperTrade.Infrastructure.Markets;

namespace PaperTrade.UnitTests.Markets;

public sealed class TwelveDataQuoteServiceTests
{
    [Fact]
    public async Task ForexQuote_UsesProviderMidAndLabelsEstimatedSpread()
    {
        var handler = new StubHandler();
        var service = new TwelveDataQuoteService(new HttpClient(handler)
        { BaseAddress = new Uri("https://example.test/") },
            Options.Create(new TwelveDataOptions { ApiKey = "test" }));

        var quote = await service.GetQuoteAsync(SupportedPairs.Create("EUR/USD")!,
            "EUR/USD", CancellationToken.None);

        Assert.Contains("quote?symbol=EUR%2FUSD", handler.Path);
        Assert.Contains("interval=1min", handler.Path);
        Assert.Contains("timezone=UTC", handler.Path);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1760000000), quote!.Timestamp);
        Assert.Equal(1.12345m, quote!.CurrentPrice);
        Assert.Equal(0.0001m, quote.Spread);
        Assert.Equal(quote.Spread, quote.Ask - quote.Bid);
        Assert.True(quote.SpreadIsSimulated);
    }

    [Fact]
    public async Task MetalQuote_UsesMinuteIntervalAndPreservesProviderTimestamp()
    {
        var handler = new StubHandler();
        var service = new TwelveDataQuoteService(new HttpClient(handler)
        { BaseAddress = new Uri("https://example.test/") },
            Options.Create(new TwelveDataOptions { ApiKey = "test" }));
        var quote = await service.GetQuoteAsync(SupportedPairs.Create("XAU/USD")!,
            "XAU/USD", CancellationToken.None);
        Assert.Contains("interval=1min", handler.Path);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1760000000), quote!.Timestamp);
    }

    [Fact]
    public async Task MissingTimestamp_DoesNotPretendAnUnknownPriceIsFresh()
    {
        var service = new TwelveDataQuoteService(new HttpClient(new StubHandler(0))
        { BaseAddress = new Uri("https://example.test/") },
            Options.Create(new TwelveDataOptions { ApiKey = "test" }));
        await Assert.ThrowsAsync<PaperTrade.Application.Markets.MarketDataUnavailableException>(() =>
            service.GetQuoteAsync(SupportedPairs.Create("XAU/USD")!, "XAU/USD", CancellationToken.None));
    }

    private sealed class StubHandler(long timestamp = 1760000000) : HttpMessageHandler
    {
        public string? Path { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Path = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$"""
                    {"close":"1.12345","open":"1.12000","high":"1.12500",
                     "low":"1.11900","previous_close":"1.12000",
                     "change":"0.00345","percent_change":"0.31","timestamp":{{timestamp}}}
                    """, Encoding.UTF8, "application/json")
            });
        }
    }
}
