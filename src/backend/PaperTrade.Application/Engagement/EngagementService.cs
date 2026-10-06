using PaperTrade.Application.Abstractions.Persistence;
using PaperTrade.Domain.Alerts;
using PaperTrade.Application.Markets;
using PaperTrade.Domain.Instruments;

namespace PaperTrade.Application.Engagement;

public sealed class EngagementService(
    IEngagementRepository repository,
    IUnitOfWork unitOfWork,
    IInstrumentCatalog instrumentCatalog) : IEngagementService
{
    public async Task<IReadOnlyList<PriceAlertDto>> GetAlertsAsync(Guid userId, CancellationToken cancellationToken) =>
        (await repository.GetAlertsAsync(userId, cancellationToken)).Select(alert => MapAlert(alert)).ToArray();

    public async Task<PriceAlertDto> CreateAlertAsync(Guid userId, CreatePriceAlertRequest request, CancellationToken cancellationToken)
    {
        var direction = Enum.Parse<PriceAlertDirection>(request.Direction, true);
        var instrument = await instrumentCatalog.GetOrCreateAsync(request.Symbol, cancellationToken);
        var alert = new PriceAlert(Guid.NewGuid(), userId, instrument.Symbol,
            direction, request.TargetPrice, DateTimeOffset.UtcNow, instrument.Id);
        repository.AddAlert(alert);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapAlert(alert, instrument);
    }

    public async Task<bool> DeleteAlertAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var alert = await repository.GetAlertAsync(id, userId, cancellationToken);
        if (alert is null) return false;
        repository.RemoveAlert(alert);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(Guid userId, CancellationToken cancellationToken) =>
        (await repository.GetNotificationsAsync(userId, cancellationToken)).Select(MapNotification).ToArray();

    public async Task<bool> MarkReadAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var notification = await repository.GetNotificationAsync(id, userId, cancellationToken);
        if (notification is null) return false;
        notification.MarkRead();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static PriceAlertDto MapAlert(PriceAlert alert, Instrument? instrument = null) =>
        new(alert.Id, alert.Symbol, alert.Direction.ToString().ToLowerInvariant(),
            alert.TargetPrice, alert.IsActive, alert.CreatedAt, alert.TriggeredAt,
            alert.InstrumentId,
            instrument is null && alert.Instrument is null ? null :
                InstrumentDto.From(instrument ?? alert.Instrument));

    private static NotificationDto MapNotification(Domain.Notifications.Notification notification) =>
        new(notification.Id, notification.Title, notification.Message,
            notification.IsRead, notification.CreatedAt);
}
