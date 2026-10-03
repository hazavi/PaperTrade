using PaperTrade.Domain.Alerts;
using PaperTrade.Domain.Notifications;

namespace PaperTrade.Application.Abstractions.Persistence;

public interface IEngagementRepository
{
    Task<IReadOnlyList<string>> GetTrackedSymbolsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PriceAlert>> GetActiveAlertsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PriceAlert>> GetAlertsAsync(Guid userId, CancellationToken cancellationToken);
    Task<PriceAlert?> GetAlertAsync(Guid id, Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Notification>> GetNotificationsAsync(Guid userId, CancellationToken cancellationToken);
    Task<Notification?> GetNotificationAsync(Guid id, Guid userId, CancellationToken cancellationToken);
    void AddAlert(PriceAlert alert);
    void RemoveAlert(PriceAlert alert);
    void AddNotification(Notification notification);
}
