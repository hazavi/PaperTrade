using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using PaperTrade.Infrastructure.Markets;

namespace PaperTrade.UnitTests.Markets;

public sealed class TwelveDataHistoryServiceTests
{
    [Fact]
    public async Task GetHistory_MapsRealOhlcAndUsesAuthorizationHeader()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "status": "ok",
                      "values": [
                        {
                          "datetime": "2026-10-05 14:30:00",
                          "open": "334.61",
                          "high": "334.76",
                          "low": "334.52",
                          "close": "334.53",
                          "volume": "125040"
                        }
                      ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });
        var service = new TwelveDataHistoryService(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") },
            Options.Create(new TwelveDataOptions
            {
                BaseUrl = "https://api.test/",
                ApiKey = "history-key"
            }));

        var prices = await service.GetHistoricalPricesAsync(
            "AAPL",
            new DateTimeOffset(2026, 10, 5, 13, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 5, 15, 0, 0, TimeSpan.Zero),
            "15",
            CancellationToken.None);

        var price = Assert.Single(prices);
        Assert.Equal(334.61m, price.Open);
        Assert.Equal(334.76m, price.High);
        Assert.Equal(334.52m, price.Low);
        Assert.Equal(334.53m, price.Close);
        Assert.Equal(125040m, price.Volume);
        Assert.NotNull(capturedRequest);
        Assert.Equal("apikey", capturedRequest.Headers.Authorization?.Scheme);
        Assert.Equal("history-key", capturedRequest.Headers.Authorization?.Parameter);
        Assert.Contains("interval=15min", capturedRequest.RequestUri?.Query);
        Assert.Contains("order=desc", capturedRequest.RequestUri?.Query);
        Assert.Contains("start_date=2026-10-05 13:00:00", Uri.UnescapeDataString(capturedRequest.RequestUri!.Query));
        Assert.DoesNotContain("history-key", capturedRequest.RequestUri?.ToString());
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}
