using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PaperTrade.Application.Abstractions.Caching;
using PaperTrade.Application.Markets;
using PaperTrade.Infrastructure.Markets;

namespace PaperTrade.UnitTests.Markets;

public sealed class CachedMarketDataServiceTests
{
    [Fact]
    public async Task GetQuote_WithCacheHit_DoesNotCallProvider()
    {
        var quote = CreateQuote();
        var cache = new FakeCacheService(quote);
        var handler = new StubHttpMessageHandler(
            _ => throw new InvalidOperationException(
                "Provider should not be called."));
        var service = CreateService(cache, handler);

        var result = await service.GetQuoteAsync(
            "aapl",
            CancellationToken.None);

        Assert.Equal(quote, result);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task GetQuote_WithCacheMiss_CallsProviderAndCachesResult()
    {
        var cache = new FakeCacheService(null);
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "c": 195.25,
                      "d": 1.25,
                      "dp": 0.64,
                      "h": 197.00,
                      "l": 192.50,
                      "o": 193.00,
                      "pc": 194.00,
                      "t": 1760000000
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            });
        var service = CreateService(cache, handler);

        var result = await service.GetQuoteAsync(
            "AAPL",
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(195.25m, result.CurrentPrice);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal(TimeSpan.FromSeconds(15), cache.LastTimeToLive);
        Assert.Equal("market:quote:AAPL", cache.LastKey);
    }

    private static CachedMarketDataService CreateService(
        FakeCacheService cache,
        HttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };

        var provider = new FinnhubMarketDataService(
            client,
            Options.Create(new FinnhubOptions
            {
                BaseUrl = "https://example.test/",
                ApiKey = "test-key"
            }));

        return new CachedMarketDataService(
            provider,
            cache,
            NullLogger<CachedMarketDataService>.Instance);
    }

    private static MarketQuote CreateQuote()
    {
        return new MarketQuote(
            "AAPL",
            195.25m,
            1.25m,
            0.64m,
            193m,
            197m,
            192.5m,
            194m,
            DateTimeOffset.UtcNow);
    }

    private sealed class FakeCacheService(object? value)
        : ICacheService
    {
        public string? LastKey { get; private set; }

        public TimeSpan? LastTimeToLive { get; private set; }

        public Task<T?> GetAsync<T>(
            string key,
            CancellationToken cancellationToken)
        {
            return Task.FromResult((T?)value);
        }

        public Task SetAsync<T>(
            string key,
            T cacheValue,
            TimeSpan timeToLive,
            CancellationToken cancellationToken)
        {
            LastKey = key;
            LastTimeToLive = timeToLive;
            return Task.CompletedTask;
        }
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(responseFactory(request));
        }
    }
}
