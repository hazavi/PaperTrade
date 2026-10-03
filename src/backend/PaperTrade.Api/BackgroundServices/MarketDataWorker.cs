using Microsoft.AspNetCore.SignalR;
using PaperTrade.Api.Realtime;
using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Application.Engagement;
using PaperTrade.Application.Markets;
using PaperTrade.Domain.Notifications;

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
        var marketData = scope.ServiceProvider.GetRequiredService<IMarketDataService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
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
                !alert.ShouldTrigger(quote.CurrentPrice)) continue;
            var now = DateTimeOffset.UtcNow;
            alert.Trigger(now);
            var notification = new Notification(Guid.NewGuid(), alert.UserId,
                $"{alert.Symbol} price alert",
                $"{alert.Symbol} reached {quote.CurrentPrice:F2} ({alert.Direction.ToString().ToLowerInvariant()} {alert.TargetPrice:F2}).",
                now);
            repository.AddNotification(notification);
            triggered.Add((alert.UserId, new NotificationDto(notification.Id,
                notification.Title, notification.Message, false, now)));
        }

        if (triggered.Count == 0) return;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        foreach (var item in triggered)
            await hub.Clients.Group($"user:{item.UserId}")
                .SendAsync("NotificationReceived", item.Notification, cancellationToken);
        logger.LogInformation("Triggered {AlertCount} price alerts", triggered.Count);
    }
}
