using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
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
    public async Task RiskCalculatorAndConcentrationLimit_UsePortfolioValueAndBlockOversizedBuy()
    {
        var email = $"risk-{Guid.NewGuid():N}@example.test";
        using var configured = ConfigureMarketData(new FakeMarketDataService { Price = 100m });
        using var client = configured.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        { HandleCookies = true });
        try
        {
            await RegisterAsync(client, email);
            var size = await (await client.PostAsJsonAsync("/api/analytics/size",
                new SizeRequest("AAPL", 100m, 95m, 1m, 110m)))
                .Content.ReadFromJsonAsync<SizeResult>();
            Assert.NotNull(size);
            Assert.Equal(200m, size.Quantity);
            Assert.Equal(2m, size.RewardRiskRatio);
            Assert.Equal(1000m, size.EstimatedRiskUsd);
            var fx = await (await client.PostAsJsonAsync("/api/analytics/size",
                new SizeRequest("USD/JPY", 150m, 149m, 1m, 152m)))
                .Content.ReadFromJsonAsync<SizeResult>();
            Assert.NotNull(fx);
            Assert.Equal(149_000m, fx.Quantity);
            Assert.Equal(100m, fx.StopDistancePips);
            Assert.Equal(100_000m, fx.LotSize);
            Assert.Equal(1000m, fx.EstimatedRiskUsd);

            Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/analytics/risk-limits",
                new RiskLimitsDto(null, 5m))).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/orders",
                new CreateOrderRequest("AAPL", "buy", "market", 100))).StatusCode);
            Assert.Empty((await client.GetFromJsonAsync<OrderDto[]>("/api/orders"))!);
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/orders",
                new CreateOrderRequest("AAPL", "buy", "market", 10))).StatusCode);
        }
        finally { await DeleteUserAsync(email); }
    }

    [Fact]
    public async Task DailyLossJournalAndExports_WorkForOwnedOrders()
    {
        var email = $"analytics-{Guid.NewGuid():N}@example.test";
        var market = new FakeMarketDataService { Price = 100m };
        using var configured = ConfigureMarketData(market);
        using var client = configured.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        { HandleCookies = true });
        try
        {
            await RegisterAsync(client, email);
            await client.PutAsJsonAsync("/api/analytics/risk-limits", new RiskLimitsDto(1m, null));
            var response = await client.PostAsJsonAsync("/api/orders",
                new CreateOrderRequest("AAPL", "buy", "market", 40));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var order = (await response.Content.ReadFromJsonAsync<OrderExecutionResult>())!.Order!;
            Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync($"/api/analytics/journal/{order.Id}",
                new { note = "Reviewed breakout setup" })).StatusCode);
            var journal = await client.GetFromJsonAsync<JournalDto[]>("/api/analytics/journal");
            Assert.Equal(order.Id, Assert.Single(journal!).OrderId);
            var csv = await client.GetStringAsync("/api/analytics/export/journal");
            Assert.Contains("Reviewed breakout setup", csv);

            market.Price = 50m;
            await using (var scope = configured.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PaperTradeDbContext>();
                var portfolioId = order.PortfolioId;
                await db.EquitySnapshots.Where(x => x.PortfolioId == portfolioId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RecordedAt,
                        DateTimeOffset.UtcNow.AddMinutes(-6)));
            }
            var performance = await client.GetFromJsonAsync<PerformanceDto>("/api/analytics/performance");
            Assert.NotNull(performance);
            Assert.True(performance.History.Count >= 2);
            Assert.True(performance.MaxDrawdownPercent > 1m);
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/orders",
                new CreateOrderRequest("AAPL", "buy", "market", 1))).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/orders",
                new CreateOrderRequest("AAPL", "sell", "market", 1))).StatusCode);
            Assert.Contains("equity_usd", await client.GetStringAsync("/api/analytics/export/portfolio"));
        }
        finally { await DeleteUserAsync(email); }
    }

    [Fact]
    public async Task DailyLimit_StartsFromCurrentEquityAfterLongObservationGap()
    {
        var email = $"gap-{Guid.NewGuid():N}@example.test";
        var market = new FakeMarketDataService { Price = 100m };
        using var configured = ConfigureMarketData(market);
        using var client = configured.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        { HandleCookies = true });
        try
        {
            await RegisterAsync(client, email);
            await client.PutAsJsonAsync("/api/analytics/risk-limits", new RiskLimitsDto(1m, null));
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/orders",
                new CreateOrderRequest("AAPL", "buy", "market", 40))).StatusCode);
            await using (var scope = configured.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PaperTradeDbContext>();
                await db.EquitySnapshots.Where(x => x.Portfolio.User.Email == email)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.RecordedAt,
                        DateTimeOffset.UtcNow.AddDays(-40)));
            }
            market.Price = 50m;
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/orders",
                new CreateOrderRequest("AAPL", "buy", "market", 1))).StatusCode);
            var performance = await client.GetFromJsonAsync<PerformanceDto>("/api/analytics/performance");
            Assert.NotNull(performance);
            Assert.True(performance.Daily.Last().Pnl > -1m);
        }
        finally { await DeleteUserAsync(email); }
    }

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
            Assert.Equal(99_599.84m, portfolio.CashBalance);
            Assert.Equal(600m, portfolio.MarketValue);
            Assert.Equal(100_199.84m, portfolio.PortfolioValue);
            Assert.Equal(99.84m, portfolio.RealizedPnl);
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

    [Fact]
    public async Task DollarYenTrade_UsesUsdCollateralAndConvertedPnl()
    {
        var email = $"fx-{Guid.NewGuid():N}@example.test";
        var marketData = new FakeMarketDataService { Price = 150m };
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
            var invalid = await client.PostAsJsonAsync("/api/orders",
                new CreateOrderRequest("USD/JPY", "buy", "market", 1500));
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

            var buy = await client.PostAsJsonAsync("/api/orders",
                new CreateOrderRequest("USD/JPY", "buy", "market", 1000));
            Assert.Equal(HttpStatusCode.Created, buy.StatusCode);
            marketData.Price = 165m;
            var portfolio = await client.GetFromJsonAsync<PortfolioDto>("/api/portfolio");
            Assert.Equal(98_999.90m, portfolio!.CashBalance);
            Assert.Equal(1_090.91m, portfolio.MarketValue);

            var sell = await client.PostAsJsonAsync("/api/orders",
                new CreateOrderRequest("USD/JPY", "sell", "market", 1000));
            Assert.Equal(HttpStatusCode.Created, sell.StatusCode);
            portfolio = await client.GetFromJsonAsync<PortfolioDto>("/api/portfolio");
            Assert.Equal(100_090.70m, portfolio!.CashBalance);
            Assert.Equal(90.70m, portfolio.RealizedPnl);
            Assert.Empty(portfolio.Positions);
        }
        finally { await DeleteUserAsync(email); }
    }

    [Fact]
    public async Task GoldUnits_UseMetalMinimumAndUsdPnl()
    {
        var email = $"gold-{Guid.NewGuid():N}@example.test";
        var marketData = new FakeMarketDataService { Price = 4000m };
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
                new CreateOrderRequest("XAU/USD", "buy", "market", 0.01m));
            Assert.Equal(HttpStatusCode.Created, buy.StatusCode);
            marketData.Price = 4100m;
            var portfolio = await client.GetFromJsonAsync<PortfolioDto>("/api/portfolio");
            Assert.Equal(99_960m, portfolio!.CashBalance);
            Assert.Equal(41m, portfolio.MarketValue);
            Assert.Equal(1m, portfolio.UnrealizedPnl);
            Assert.Equal("metal", Assert.Single(portfolio.Positions).Instrument!.AssetClass);
        }
        finally { await DeleteUserAsync(email); }
    }

    [Fact]
    public async Task LimitOrder_WaitsForPriceThenRecordsExecution()
    {
        var email = $"limit-{Guid.NewGuid():N}@example.test";
        var marketData = new FakeMarketDataService { Price = 100m };
        using var configuredFactory = ConfigureMarketData(marketData);
        using var client = configuredFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        { HandleCookies = true });
        try
        {
            await RegisterAsync(client, email);
            var response = await client.PostAsJsonAsync("/api/orders",
                new CreateOrderRequest("AAPL", "buy", "limit", 10, Price: 95m));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var submitted = await response.Content.ReadFromJsonAsync<OrderExecutionResult>();
            Assert.Equal(OrderExecutionStatus.Pending, submitted!.Status);
            Assert.Equal("pending", submitted.Order!.Status);
            Assert.Equal(100_000m, (await client.GetFromJsonAsync<PortfolioDto>("/api/portfolio"))!.CashBalance);

            marketData.Price = 94m;
            await ProcessPendingAsync(configuredFactory.Services);
            var order = Assert.Single((await client.GetFromJsonAsync<OrderDto[]>("/api/orders"))!);
            Assert.Equal("filled", order.Status);
            Assert.Equal(94m, order.ExecutedPrice);
            Assert.Equal(10m, order.FilledQuantity);
            var execution = Assert.Single((await client.GetFromJsonAsync<ExecutionDto[]>("/api/orders/executions"))!);
            Assert.Equal(0.09m, execution.Fee);
            Assert.Equal(94m, execution.QuotePrice);
            await using var scope = configuredFactory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PaperTradeDbContext>();
            await Assert.ThrowsAsync<PostgresException>(() => db.Trades
                .Where(trade => trade.Id == execution.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(trade => trade.Fee, 0m)));
            await Assert.ThrowsAsync<PostgresException>(() => db.Trades
                .Where(trade => trade.Id == execution.Id).ExecuteDeleteAsync());
        }
        finally { await DeleteUserAsync(email); }
    }

    [Fact]
    public async Task PendingOrder_CanBeCancelledWithoutAFill()
    {
        var email = $"cancel-{Guid.NewGuid():N}@example.test";
        var marketData = new FakeMarketDataService { Price = 100m };
        using var configuredFactory = ConfigureMarketData(marketData);
        using var client = configuredFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        { HandleCookies = true });
        try
        {
            await RegisterAsync(client, email);
            var response = await client.PostAsJsonAsync("/api/orders",
                new CreateOrderRequest("AAPL", "buy", "stop", 2, Price: 110m));
            var submitted = await response.Content.ReadFromJsonAsync<OrderExecutionResult>();
            Assert.Equal(HttpStatusCode.NoContent,
                (await client.DeleteAsync($"/api/orders/{submitted!.Order!.Id}")).StatusCode);
            marketData.Price = 120m;
            await ProcessPendingAsync(configuredFactory.Services);
            Assert.Equal("cancelled", Assert.Single((await client.GetFromJsonAsync<OrderDto[]>("/api/orders"))!).Status);
            Assert.Empty((await client.GetFromJsonAsync<ExecutionDto[]>("/api/orders/executions"))!);
        }
        finally { await DeleteUserAsync(email); }
    }

    [Fact]
    public async Task BracketOrder_FillsOneExitAndCancelsTheOther()
    {
        var email = $"bracket-{Guid.NewGuid():N}@example.test";
        var marketData = new FakeMarketDataService { Price = 100m };
        using var configuredFactory = ConfigureMarketData(marketData);
        using var client = configuredFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        { HandleCookies = true });
        try
        {
            await RegisterAsync(client, email);
            var response = await client.PostAsJsonAsync("/api/orders",
                new CreateOrderRequest("AAPL", "buy", "bracket", 10,
                    TakeProfit: 110m, StopLoss: 90m));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var orders = (await client.GetFromJsonAsync<OrderDto[]>("/api/orders"))!;
            Assert.Equal(3, orders.Length);
            var parent = Assert.Single(orders, order => order.Type == "bracket");
            Assert.Equal("filled", parent.Status);
            Assert.All(orders.Where(order => order.ParentOrderId == parent.Id),
                child => Assert.Equal("pending", child.Status));

            marketData.Price = 111m;
            await ProcessPendingAsync(configuredFactory.Services);
            orders = (await client.GetFromJsonAsync<OrderDto[]>("/api/orders"))!;
            Assert.Equal("filled", Assert.Single(orders, order => order.Type == "limit").Status);
            Assert.Equal("cancelled", Assert.Single(orders, order => order.Type == "stop").Status);
            Assert.Empty((await client.GetFromJsonAsync<PortfolioDto>("/api/portfolio"))!.Positions);
            Assert.Equal(2, (await client.GetFromJsonAsync<ExecutionDto[]>("/api/orders/executions"))!.Length);
        }
        finally { await DeleteUserAsync(email); }
    }

    [Fact]
    public async Task MarketOrder_UsesAskAndConfiguredSlippage()
    {
        var email = $"slippage-{Guid.NewGuid():N}@example.test";
        var marketData = new FakeMarketDataService { Price = 100m, Spread = 0.2m };
        using var configuredFactory = ConfigureMarketData(marketData);
        using var client = configuredFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        { HandleCookies = true });
        try
        {
            await RegisterAsync(client, email);
            configuredFactory.Services.GetRequiredService<TradingSimulationOptions>().SlippageBps = 10m;
            var response = await client.PostAsJsonAsync("/api/orders",
                new CreateOrderRequest("AAPL", "buy", "market", 10));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var execution = Assert.Single((await client.GetFromJsonAsync<ExecutionDto[]>("/api/orders/executions"))!);
            Assert.Equal(100.1m, execution.QuotePrice);
            Assert.Equal(100.21m, execution.Price);
            Assert.Equal(0.10m, execution.Fee);
        }
        finally { await DeleteUserAsync(email); }
    }

    [Fact]
    public async Task PendingOrder_ExpiresWithoutExecuting()
    {
        var email = $"expiry-{Guid.NewGuid():N}@example.test";
        var marketData = new FakeMarketDataService { Price = 100m };
        using var configuredFactory = ConfigureMarketData(marketData);
        using var client = configuredFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        { HandleCookies = true });
        try
        {
            await RegisterAsync(client, email);
            var response = await client.PostAsJsonAsync("/api/orders",
                new CreateOrderRequest("AAPL", "buy", "limit", 1, Price: 90m,
                    ExpiresAt: DateTimeOffset.UtcNow.AddHours(1)));
            var submitted = await response.Content.ReadFromJsonAsync<OrderExecutionResult>();
            await using (var scope = configuredFactory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PaperTradeDbContext>();
                await db.Orders.Where(order => order.Id == submitted!.Order!.Id)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(order => order.ExpiresAt,
                        DateTimeOffset.UtcNow.AddSeconds(-1)));
            }
            marketData.Price = 80m;
            await ProcessPendingAsync(configuredFactory.Services);
            Assert.Equal("expired", Assert.Single((await client.GetFromJsonAsync<OrderDto[]>("/api/orders"))!).Status);
            Assert.Empty((await client.GetFromJsonAsync<ExecutionDto[]>("/api/orders/executions"))!);
        }
        finally { await DeleteUserAsync(email); }
    }

    [Fact]
    public async Task TriggeredBuyWithoutCash_IsRejected()
    {
        var email = $"rejected-{Guid.NewGuid():N}@example.test";
        var marketData = new FakeMarketDataService { Price = 100m };
        using var configuredFactory = ConfigureMarketData(marketData);
        using var client = configuredFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        { HandleCookies = true });
        try
        {
            await RegisterAsync(client, email);
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/orders",
                new CreateOrderRequest("AAPL", "buy", "limit", 1000, Price: 95m))).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/orders",
                new CreateOrderRequest("AAPL", "buy", "market", 500))).StatusCode);
            marketData.Price = 94m;
            await ProcessPendingAsync(configuredFactory.Services);
            var order = Assert.Single((await client.GetFromJsonAsync<OrderDto[]>("/api/orders"))!,
                item => item.Type == "limit");
            Assert.Equal("rejected", order.Status);
            Assert.Single((await client.GetFromJsonAsync<ExecutionDto[]>("/api/orders/executions"))!);
        }
        finally { await DeleteUserAsync(email); }
    }

    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> ConfigureMarketData(
        FakeMarketDataService marketData) => factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IMarketDataService>();
                services.AddSingleton<IMarketDataService>(marketData);
            }));

    private static async Task ProcessPendingAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ITradingService>()
            .ProcessPendingOrdersAsync(CancellationToken.None);
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
        public decimal Spread { get; set; }
        public Task<MarketQuote?> GetQuoteAsync(string symbol, CancellationToken cancellationToken) =>
            Task.FromResult<MarketQuote?>(new MarketQuote(symbol.ToUpperInvariant(),
                Price, 0, 0, Price, Price, Price, Price, DateTimeOffset.UtcNow,
                Spread > 0 ? Price - Spread / 2 : null,
                Spread > 0 ? Price + Spread / 2 : null,
                Spread > 0 ? Spread : null));
        public Task<IReadOnlyList<AssetSummary>> SearchAssetsAsync(string query, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AssetSummary>>([]);
        public Task<IReadOnlyList<HistoricalPrice>> GetHistoricalPricesAsync(string symbol, DateTimeOffset from, DateTimeOffset to, string resolution, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<HistoricalPrice>>([]);
        public Task<MarketStatus> GetMarketStatusAsync(string exchange, CancellationToken cancellationToken) =>
            Task.FromResult(new MarketStatus(exchange, true, "regular", "UTC", DateTimeOffset.UtcNow));
    }
}
