using IsoDocs.Application.Common.Messaging;
using MediatR;

namespace IsoDocs.Application.Communications.Commands;

/// <summary>
/// 將目前使用者所有未讀通知一次標記為已讀。
/// </summary>
public sealed record MarkAllNotificationsReadCommand(Guid CurrentUserId) : ICommand;

public sealed class MarkAllNotificationsReadCommandHandler
    : ICommandHandler<MarkAllNotificationsReadCommand>
{
    private readonly INotificationRepository _notifications;

    public MarkAllNotificationsReadCommandHandler(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<Unit> Handle(
        MarkAllNotificationsReadCommand request,
        CancellationToken cancellationToken)
    {
        var unread = await _notifications.ListUnreadTrackedAsync(
            request.CurrentUserId,
            cancellationToken);

        foreach (var n in unread)
            n.MarkRead();

        await _notifications.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
