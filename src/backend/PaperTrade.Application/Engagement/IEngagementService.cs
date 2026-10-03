namespace PaperTrade.Application.Engagement;

public interface IEngagementService
{
    Task<IReadOnlyList<PriceAlertDto>> GetAlertsAsync(Guid userId, CancellationToken cancellationToken);
    Task<PriceAlertDto> CreateAlertAsync(Guid userId, CreatePriceAlertRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAlertAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> MarkReadAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}
