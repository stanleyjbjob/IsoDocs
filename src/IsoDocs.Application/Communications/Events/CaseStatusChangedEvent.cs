using IsoDocs.Domain.Communications;
using MediatR;

namespace IsoDocs.Application.Communications.Events;

/// <summary>
/// 案件狀態變更時通知發起人。
/// </summary>
public sealed record CaseStatusChangedEvent(
    Guid CaseId,
    string CaseNumber,
    Guid InitiatorUserId,
    string NewStatus) : INotification;

public sealed class CaseStatusChangedEventHandler : INotificationHandler<CaseStatusChangedEvent>
{
    private readonly INotificationRepository _notifications;

    public CaseStatusChangedEventHandler(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task Handle(CaseStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        var entity = new Notification(
            id: Guid.NewGuid(),
            recipientUserId: notification.InitiatorUserId,
            type: NotificationType.CaseStatusChanged,
            channel: NotificationChannel.InApp,
            subject: $"您的案件狀態已更新：{notification.CaseNumber}",
            body: $"案件 {notification.CaseNumber} 狀態變更為「{notification.NewStatus}」。",
            caseId: notification.CaseId,
            payloadJson: null);

        entity.MarkSent();
        await _notifications.AddAsync(entity, cancellationToken);
        await _notifications.SaveChangesAsync(cancellationToken);
    }
}
