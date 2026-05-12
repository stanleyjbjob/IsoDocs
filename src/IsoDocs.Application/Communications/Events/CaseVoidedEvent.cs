using IsoDocs.Domain.Communications;
using MediatR;

namespace IsoDocs.Application.Communications.Events;

/// <summary>
/// 主單作廢時通知相關人員。
/// </summary>
public sealed record CaseVoidedEvent(
    Guid CaseId,
    string CaseNumber,
    IReadOnlyList<Guid> RecipientUserIds,
    string Reason) : INotification;

public sealed class CaseVoidedEventHandler : INotificationHandler<CaseVoidedEvent>
{
    private readonly INotificationRepository _notifications;

    public CaseVoidedEventHandler(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task Handle(CaseVoidedEvent notification, CancellationToken cancellationToken)
    {
        foreach (var userId in notification.RecipientUserIds.Distinct())
        {
            var entity = new Notification(
                id: Guid.NewGuid(),
                recipientUserId: userId,
                type: NotificationType.Voided,
                channel: NotificationChannel.InApp,
                subject: $"案件已作廢：{notification.CaseNumber}",
                body: $"案件 {notification.CaseNumber} 已被作廢。原因：{notification.Reason}",
                caseId: notification.CaseId,
                payloadJson: null);

            entity.MarkSent();
            await _notifications.AddAsync(entity, cancellationToken);
        }

        await _notifications.SaveChangesAsync(cancellationToken);
    }
}
