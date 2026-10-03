using Microsoft.EntityFrameworkCore;
using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Domain.Alerts;
using PaperTrade.Domain.Notifications;

namespace PaperTrade.Infrastructure.Persistence.Repositories;

internal sealed class EngagementRepository(PaperTradeDbContext dbContext)
    : IEngagementRepository
{
    public async Task<IReadOnlyList<string>> GetTrackedSymbolsAsync(CancellationToken cancellationToken)
    {
        var positionSymbols = dbContext.Positions.Select(position => position.Symbol);
        var watchlistSymbols = dbContext.WatchlistItems.Select(item => item.Symbol);
        var alertSymbols = dbContext.PriceAlerts.Where(alert => alert.IsActive).Select(alert => alert.Symbol);
        return await positionSymbols.Concat(watchlistSymbols).Concat(alertSymbols)
            .Distinct().OrderBy(symbol => symbol).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PriceAlert>> GetActiveAlertsAsync(CancellationToken cancellationToken) =>
        await dbContext.PriceAlerts.Where(alert => alert.IsActive).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PriceAlert>> GetAlertsAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.PriceAlerts.AsNoTracking().Where(alert => alert.UserId == userId)
            .OrderByDescending(alert => alert.CreatedAt).ToListAsync(cancellationToken);

    public Task<PriceAlert?> GetAlertAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        dbContext.PriceAlerts.SingleOrDefaultAsync(alert => alert.Id == id && alert.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<Notification>> GetNotificationsAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.Notifications.AsNoTracking().Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAt).Take(100).ToListAsync(cancellationToken);

    public Task<Notification?> GetNotificationAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        dbContext.Notifications.SingleOrDefaultAsync(notification => notification.Id == id && notification.UserId == userId, cancellationToken);

    public void AddAlert(PriceAlert alert) => dbContext.PriceAlerts.Add(alert);
    public void RemoveAlert(PriceAlert alert) => dbContext.PriceAlerts.Remove(alert);
    public void AddNotification(Notification notification) => dbContext.Notifications.Add(notification);
}
