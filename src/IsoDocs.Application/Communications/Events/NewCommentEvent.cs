using IsoDocs.Domain.Communications;
using MediatR;

namespace IsoDocs.Application.Communications.Events;

/// <summary>
/// 案件新增留言時通知相關人員。
/// </summary>
public sealed record NewCommentEvent(
    Guid CaseId,
    string CaseNumber,
    IReadOnlyList<Guid> RecipientUserIds,
    string CommenterName) : INotification;

public sealed class NewCommentEventHandler : INotificationHandler<NewCommentEvent>
{
    private readonly INotificationRepository _notifications;

    public NewCommentEventHandler(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task Handle(NewCommentEvent notification, CancellationToken cancellationToken)
    {
        foreach (var userId in notification.RecipientUserIds.Distinct())
        {
            var entity = new Notification(
                id: Guid.NewGuid(),
                recipientUserId: userId,
                type: NotificationType.NewComment,
                channel: NotificationChannel.InApp,
                subject: $"案件 {notification.CaseNumber} 有新留言",
                body: $"{notification.CommenterName} 在案件 {notification.CaseNumber} 新增了留言。",
                caseId: notification.CaseId,
                payloadJson: null);

            entity.MarkSent();
            await _notifications.AddAsync(entity, cancellationToken);
        }

        await _notifications.SaveChangesAsync(cancellationToken);
    }
}
