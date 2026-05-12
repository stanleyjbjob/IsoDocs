using IsoDocs.Domain.Communications;
using MediatR;

namespace IsoDocs.Application.Communications.Events;

/// <summary>
/// 節點流轉到指定使用者時發布此事件。
/// </summary>
public sealed record NodeAssignedEvent(
    Guid CaseId,
    string CaseNumber,
    Guid AssignedToUserId,
    string NodeName) : INotification;

public sealed class NodeAssignedEventHandler : INotificationHandler<NodeAssignedEvent>
{
    private readonly INotificationRepository _notifications;

    public NodeAssignedEventHandler(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task Handle(NodeAssignedEvent notification, CancellationToken cancellationToken)
    {
        var entity = new Notification(
            id: Guid.NewGuid(),
            recipientUserId: notification.AssignedToUserId,
            type: NotificationType.NodeAssigned,
            channel: NotificationChannel.InApp,
            subject: $"您有新的待處理節點：{notification.NodeName}",
            body: $"案件 {notification.CaseNumber} 的節點「{notification.NodeName}」已指派給您，請盡快處理。",
            caseId: notification.CaseId,
            payloadJson: null);

        entity.MarkSent();
        await _notifications.AddAsync(entity, cancellationToken);
        await _notifications.SaveChangesAsync(cancellationToken);
    }
}
