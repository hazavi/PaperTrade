using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PaperTrade.Application.Authentication;
using PaperTrade.Application.Markets;
using PaperTrade.Domain.Assets;
using PaperTrade.Infrastructure.Persistence;
using PaperTrade.IntegrationTests.Authentication;

namespace PaperTrade.IntegrationTests.Markets;

public sealed class MarketEndpointsTests(
    PaperTradeApiFactory factory)
    : IClassFixture<PaperTradeApiFactory>
{
    private const string Password = "a-long-passphrase";

    [Fact]
    public async Task Search_WithAuthenticatedUser_ReturnsProviderResults()
    {
        var email = CreateUniqueEmail();
        using var configuredFactory = CreateConfiguredFactory();
        using var client = configuredFactory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                HandleCookies = true
            });

        try
        {
            await RegisterAsync(client, email);

            var response = await client.GetAsync(
                "/api/markets/search?q=apple");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var assets = await response.Content
                .ReadFromJsonAsync<AssetSummary[]>();

            var asset = Assert.Single(assets!);
            Assert.Equal("AAPL", asset.Symbol);
            var instrument = await client.GetFromJsonAsync<InstrumentDto>(
                "/api/markets/AAPL/instrument");
            Assert.Equal(asset.Symbol, instrument!.Symbol);
            Assert.Equal("Apple Inc.", instrument.DisplayName);
            Assert.NotEqual(Guid.Empty, instrument.Id);
            await using var scope = configuredFactory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PaperTradeDbContext>();
            Assert.Equal(2, await db.ProviderSymbols.CountAsync(mapping =>
                mapping.InstrumentId == instrument.Id));
        }
        finally
        {
            await DeleteUserAsync(email);
        }
    }

    [Fact]
    public async Task Quote_WithoutSession_ReturnsUnauthorized()
    {
        using var configuredFactory = CreateConfiguredFactory();
        using var client = configuredFactory.CreateClient();

        var response = await client.GetAsync(
            "/api/markets/AAPL/quote");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task PairSearchAndInstrument_ExposeFxAndMetalContracts()
    {
        var email = CreateUniqueEmail();
        using var configuredFactory = CreateConfiguredFactory();
        using var client = configuredFactory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            { HandleCookies = true });
        try
        {
            await RegisterAsync(client, email);
            var results = await client.GetFromJsonAsync<InstrumentDto[]>(
                "/api/markets/search?q=USD%2FJPY");
            var yen = Assert.Single(results!);
            Assert.Equal("forex", yen.AssetClass);
            Assert.Equal("JPY", yen.QuoteCurrency);
            Assert.Equal(0.01m, yen.PipSize);
            Assert.Equal(100000m, yen.LotSize);

            var gold = await client.GetFromJsonAsync<InstrumentDto>(
                "/api/markets/pair/XAU/USD/instrument");
            Assert.Equal("metal", gold!.AssetClass);
            Assert.Equal("XAU/USD", gold.Symbol);
            Assert.Equal(0.01m, gold.MinimumOrderSize);
            var quote = await client.GetFromJsonAsync<MarketQuote>(
                "/api/markets/pair/EUR/USD/quote");
            Assert.Equal("EUR/USD", quote!.Symbol);
        }
        finally { await DeleteUserAsync(email); }
    }

    [Fact]
    public async Task History_WithNoProviderData_ReturnsEmptyArray()
    {
        var email = CreateUniqueEmail();
        using var configuredFactory = CreateConfiguredFactory();
        using var client = configuredFactory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing
                .WebApplicationFactoryClientOptions
            {
                HandleCookies = true
            });

        try
        {
            await RegisterAsync(client, email);

            var response = await client.GetAsync(
                "/api/markets/AAPL/history?timeframe=1M");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var prices = await response.Content
                .ReadFromJsonAsync<HistoricalPrice[]>();
            Assert.Empty(prices!);
        }
        finally
        {
            await DeleteUserAsync(email);
        }
    }

    private Microsoft.AspNetCore.Mvc.Testing
        .WebApplicationFactory<Program> CreateConfiguredFactory()
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IMarketDataService>();
                services.AddSingleton<
                    IMarketDataService,
                    FakeMarketDataService>();
            });
        });
    }

    private static Task<HttpResponseMessage> RegisterAsync(
        HttpClient client,
        string email)
    {
        return client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, Password, "Market Tester"));
    }

    private async Task DeleteUserAsync(string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<PaperTradeDbContext>();

        await dbContext.Users
            .Where(user => user.Email == email)
            .ExecuteDeleteAsync();
    }

    private static string CreateUniqueEmail()
    {
        return $"market-{Guid.NewGuid():N}@example.test";
    }

    private sealed class FakeMarketDataService
        : IMarketDataService
    {
        public Task<IReadOnlyList<AssetSummary>> SearchAssetsAsync(
            string query,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<AssetSummary> results =
            [
                new AssetSummary(
                    "AAPL",
                    "Apple Inc.",
                    "NASDAQ",
                    AssetType.Stock,
                    "USD")
            ];

            return Task.FromResult(results);
        }

        public Task<MarketQuote?> GetQuoteAsync(
            string symbol,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<MarketQuote?>(symbol == "EUR/USD"
                ? new MarketQuote(symbol, 1.1m, 0, 0, 1.1m, 1.1m, 1.1m,
                    1.1m, DateTimeOffset.UtcNow, 1.09995m, 1.10005m, 0.0001m, true)
                : null);
        }

        public Task<IReadOnlyList<HistoricalPrice>>
            GetHistoricalPricesAsync(
                string symbol,
                DateTimeOffset from,
                DateTimeOffset to,
                string resolution,
                CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<HistoricalPrice>>(
                []);
        }

        public Task<MarketStatus> GetMarketStatusAsync(
            string exchange,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new MarketStatus(
                exchange,
                true,
                "regular",
                "America/New_York",
                DateTimeOffset.UtcNow));
        }
    }
}
