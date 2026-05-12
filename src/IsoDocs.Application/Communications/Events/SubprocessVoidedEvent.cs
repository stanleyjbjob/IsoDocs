using IsoDocs.Domain.Communications;
using MediatR;

namespace IsoDocs.Application.Communications.Events;

/// <summary>
/// 子流程獨立作廢時通知相關人員。
/// </summary>
public sealed record SubprocessVoidedEvent(
    Guid CaseId,
    string CaseNumber,
    Guid ParentCaseId,
    string ParentCaseNumber,
    IReadOnlyList<Guid> RecipientUserIds) : INotification;

public sealed class SubprocessVoidedEventHandler : INotificationHandler<SubprocessVoidedEvent>
{
    private readonly INotificationRepository _notifications;

    public SubprocessVoidedEventHandler(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task Handle(SubprocessVoidedEvent notification, CancellationToken cancellationToken)
    {
        foreach (var userId in notification.RecipientUserIds.Distinct())
        {
            var entity = new Notification(
                id: Guid.NewGuid(),
                recipientUserId: userId,
                type: NotificationType.SubprocessVoided,
                channel: NotificationChannel.InApp,
                subject: $"子流程已獨立作廢：{notification.CaseNumber}",
                body: $"主單 {notification.ParentCaseNumber} 的子流程 {notification.CaseNumber} 已獨立作廢。",
                caseId: notification.CaseId,
                payloadJson: null);

            entity.MarkSent();
            await _notifications.AddAsync(entity, cancellationToken);
        }

        await _notifications.SaveChangesAsync(cancellationToken);
    }
}
