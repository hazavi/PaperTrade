using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PaperTrade.Application.Authentication;
using PaperTrade.Application.Markets;
using PaperTrade.Application.Portfolios;
using PaperTrade.Application.Trading;
using PaperTrade.Domain.Assets;
using PaperTrade.Infrastructure.Persistence;
using PaperTrade.IntegrationTests.Authentication;

namespace PaperTrade.IntegrationTests.Trading;

public sealed class TradingFlowTests(PaperTradeApiFactory factory)
    : IClassFixture<PaperTradeApiFactory>
{
    [Fact]
    public async Task BuyAndSell_UpdatesCashPositionPnlAndHistory()
    {
        var email = $"trading-{Guid.NewGuid():N}@example.test";
        var marketData = new FakeMarketDataService { Price = 100m };
        using var configuredFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IMarketDataService>();
                services.AddSingleton<IMarketDataService>(marketData);
            }));
        using var client = configuredFactory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            { HandleCookies = true });

        try
        {
            await RegisterAsync(client, email);

            var buy = await client.PostAsJsonAsync("/api/orders",
                new CreateOrderRequest("aapl", "buy", "market", 10));
            Assert.Equal(HttpStatusCode.Created, buy.StatusCode);

            marketData.Price = 120m;
            var sell = await client.PostAsJsonAsync("/api/orders",
                new CreateOrderRequest("AAPL", "sell", "market", 5));
            Assert.Equal(HttpStatusCode.Created, sell.StatusCode);

            var portfolio = await client.GetFromJsonAsync<PortfolioDto>("/api/portfolio");
            Assert.NotNull(portfolio);
            Assert.Equal(99_600m, portfolio.CashBalance);
            Assert.Equal(600m, portfolio.MarketValue);
            Assert.Equal(100_200m, portfolio.PortfolioValue);
            Assert.Equal(100m, portfolio.RealizedPnl);
            Assert.Equal(100m, portfolio.UnrealizedPnl);
            Assert.Equal(0.20m, portfolio.TotalReturnPercentage);
            var position = Assert.Single(portfolio.Positions);
            Assert.Equal(5m, position.Quantity);
            Assert.Equal(100m, position.AverageEntryPrice);
            Assert.NotEqual(Guid.Empty, position.InstrumentId);
            Assert.Equal("AAPL", position.Instrument!.Symbol);

            var orders = await client.GetFromJsonAsync<OrderDto[]>("/api/orders");
            Assert.Equal(2, orders!.Length);
            Assert.All(orders, order => Assert.Equal("filled", order.Status));
            Assert.All(orders, order => Assert.Equal(position.InstrumentId, order.InstrumentId));
        }
        finally
        {
            await DeleteUserAsync(email);
        }
    }

    [Fact]
    public async Task BuyBeyondCash_IsRejectedWithoutPersistingOrder()
    {
        var email = $"funds-{Guid.NewGuid():N}@example.test";
        var marketData = new FakeMarketDataService { Price = 100m };
        using var configuredFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IMarketDataService>();
                services.AddSingleton<IMarketDataService>(marketData);
            }));
        using var client = configuredFactory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            { HandleCookies = true });

        try
        {
            await RegisterAsync(client, email);
            var response = await client.PostAsJsonAsync("/api/orders",
                new CreateOrderRequest("AAPL", "buy", "market", 1001));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var portfolio = await client.GetFromJsonAsync<PortfolioDto>("/api/portfolio");
            Assert.Equal(100_000m, portfolio!.CashBalance);
            Assert.Empty(portfolio.Positions);
            Assert.Empty((await client.GetFromJsonAsync<OrderDto[]>("/api/orders"))!);
        }
        finally
        {
            await DeleteUserAsync(email);
        }
    }

    private static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email) =>
        client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "a-long-passphrase", "Trading Tester"));

    private async Task DeleteUserAsync(string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PaperTradeDbContext>();
        await db.Users.Where(user => user.Email == email).ExecuteDeleteAsync();
    }

    private sealed class FakeMarketDataService : IMarketDataService
    {
        public decimal Price { get; set; }
        public Task<MarketQuote?> GetQuoteAsync(string symbol, CancellationToken cancellationToken) =>
            Task.FromResult<MarketQuote?>(new MarketQuote(symbol.ToUpperInvariant(),
                Price, 0, 0, Price, Price, Price, Price, DateTimeOffset.UtcNow));
        public Task<IReadOnlyList<AssetSummary>> SearchAssetsAsync(string query, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AssetSummary>>([]);
        public Task<IReadOnlyList<HistoricalPrice>> GetHistoricalPricesAsync(string symbol, DateTimeOffset from, DateTimeOffset to, string resolution, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<HistoricalPrice>>([]);
        public Task<MarketStatus> GetMarketStatusAsync(string exchange, CancellationToken cancellationToken) =>
            Task.FromResult(new MarketStatus(exchange, true, "regular", "UTC", DateTimeOffset.UtcNow));
    }
}
