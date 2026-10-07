using Microsoft.AspNetCore.SignalR;
using PaperTrade.Api.Realtime;
using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Application.Engagement;
using PaperTrade.Application.Markets;
using PaperTrade.Domain.Notifications;
using PaperTrade.Application.Trading;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using PaperTrade.Infrastructure.Persistence;

namespace PaperTrade.Api.BackgroundServices;

public sealed class MarketDataWorker(
    IServiceScopeFactory scopeFactory,
    IHubContext<MarketHub> hub,
    MarketSubscriptionTracker tracker,
    IConfiguration configuration,
    ILogger<MarketDataWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var seconds = Math.Max(5,
            configuration.GetValue("MarketDataWorker:IntervalSeconds", 15));
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));
        while (await timer.WaitForNextTickAsync(stoppingToken))
            await PollAsync(stoppingToken);
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IEngagementRepository>();
        var instruments = scope.ServiceProvider.GetRequiredService<IInstrumentRepository>();
        var marketData = scope.ServiceProvider.GetRequiredService<IMarketDataService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var db = scope.ServiceProvider.GetRequiredService<PaperTradeDbContext>();
        var trading = scope.ServiceProvider.GetRequiredService<ITradingService>();
        try { await trading.ProcessPendingOrdersAsync(cancellationToken); }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Pending order processing failed");
        }
        try { await trading.ProcessMarginAsync(cancellationToken); }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Margin processing failed");
        }
        var tracked = await repository.GetTrackedSymbolsAsync(cancellationToken);
        var symbols = tracked.Concat(tracker.GetSymbols())
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (symbols.Length == 0) return;

        var quotes = new Dictionary<string, MarketQuote>(StringComparer.OrdinalIgnoreCase);
        foreach (var symbol in symbols)
        {
            try
            {
                var quote = await marketData.GetQuoteAsync(symbol, cancellationToken);
                if (quote is null) continue;
                quotes[symbol] = quote;
                await hub.Clients.Group($"market:{symbol}")
                    .SendAsync("QuoteUpdated", quote, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Market worker failed to refresh {Symbol}", symbol);
            }
        }

        var alerts = await repository.GetActiveAlertsAsync(cancellationToken);
        var triggered = new List<(Guid UserId, NotificationDto Notification)>();
        foreach (var alert in alerts)
        {
            if (!quotes.TryGetValue(alert.Symbol, out var quote) ||
                DateTimeOffset.UtcNow - quote.Timestamp > TimeSpan.FromMinutes(15)) continue;
            decimal? observed = alert.Metric switch
            {
                "price" => quote.CurrentPrice,
                "percentChange" => quote.PercentChange,
                "volume" or "sma20" => await ObserveHistoryMetricAsync(alert.Symbol, alert.Metric, marketData, cancellationToken),
                _ => null
            };
            if (observed is null || !alert.ShouldTrigger(observed.Value)) continue;
            var now = DateTimeOffset.UtcNow;
            alert.Trigger(now);
            var instrument = await instruments.GetBySymbolAsync(alert.Symbol, cancellationToken);
            var precision = instrument?.PricePrecision ?? 2;
            var currentPrice = observed.Value.ToString($"F{precision}", CultureInfo.InvariantCulture);
            var targetPrice = alert.TargetPrice.ToString($"F{precision}", CultureInfo.InvariantCulture);
            var notification = new Notification(Guid.NewGuid(), alert.UserId,
                $"{alert.Symbol} {alert.Metric} alert",
                $"{alert.Symbol} {alert.Metric} reached {currentPrice} ({alert.Direction.ToString().ToLowerInvariant()} {targetPrice}).",
                now);
            repository.AddNotification(notification);
            triggered.Add((alert.UserId, new NotificationDto(notification.Id,
                notification.Title, notification.Message, false, now)));
        }

        if (triggered.Count == 0) return;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        foreach (var item in triggered)
        {
            await hub.Clients.Group($"user:{item.UserId}")
                .SendAsync("NotificationReceived", item.Notification, cancellationToken);
            await SendOptInEmailAsync(db, item.UserId, item.Notification, cancellationToken);
        }
        logger.LogInformation("Triggered {AlertCount} price alerts", triggered.Count);
    }

    private static async Task<decimal?> ObserveHistoryMetricAsync(string symbol, string metric,
        IMarketDataService marketData, CancellationToken token)
    {
        var now = DateTimeOffset.UtcNow;
        var bars = await marketData.GetHistoricalPricesAsync(symbol, now.AddDays(-45), now, "D", token);
        if (bars.Count == 0) return null;
        var recent = bars.OrderBy(x => x.Time).TakeLast(20).ToArray();
        return metric == "volume" ? recent[^1].Volume :
            recent.Length < 20 ? null : recent.Average(x => x.Close);
    }

    private async Task SendOptInEmailAsync(PaperTradeDbContext db, Guid userId, NotificationDto notification,
        CancellationToken token)
    {
        var host = configuration["Alerts:Email:Host"];
        var sender = configuration["Alerts:Email:From"];
        if (!configuration.GetValue("Alerts:Email:Enabled", false) ||
            string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(sender)) return;
        var user = await db.Users.AsNoTracking().Where(x => x.Id == userId)
            .Select(x => new { x.Email, x.EmailAlertsEnabled }).SingleOrDefaultAsync(token);
        if (user?.EmailAlertsEnabled != true) return;
        try
        {
            using var client = new SmtpClient(host, configuration.GetValue("Alerts:Email:Port", 587))
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(configuration["Alerts:Email:Username"], configuration["Alerts:Email:Password"])
            };
            using var message = new MailMessage(sender, user.Email, notification.Title, notification.Message);
            await client.SendMailAsync(message, token);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Alert email delivery failed for user {UserId}", userId);
        }
    }
}
