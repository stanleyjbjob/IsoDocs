using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;
using MediatR;

namespace IsoDocs.Application.Communications.Commands;

/// <summary>
/// 將指定通知標記為已讀。只有通知的收件人可執行。
/// </summary>
public sealed record MarkNotificationReadCommand(
    Guid NotificationId,
    Guid CurrentUserId) : ICommand;

public sealed class MarkNotificationReadCommandHandler
    : ICommandHandler<MarkNotificationReadCommand>
{
    private readonly INotificationRepository _notifications;

    public MarkNotificationReadCommandHandler(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<Unit> Handle(
        MarkNotificationReadCommand request,
        CancellationToken cancellationToken)
    {
        var notification = await _notifications.FindByIdAsync(request.NotificationId, cancellationToken)
            ?? throw new DomainException($"通知 {request.NotificationId} 不存在。");

        if (notification.RecipientUserId != request.CurrentUserId)
            throw new DomainException("無權限標記他人的通知。");

        notification.MarkRead();
        await _notifications.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
