using IsoDocs.Application.Common.Messaging;

namespace IsoDocs.Application.Communications.Queries;

/// <summary>
/// 列出目前使用者的通知清單，支援未讀篩選與分頁。
/// </summary>
public sealed record ListNotificationsQuery(
    Guid CurrentUserId,
    bool UnreadOnly = false,
    int Page = 1,
    int PageSize = 20) : IQuery<ListNotificationsResult>;

public sealed record ListNotificationsResult(
    IReadOnlyList<NotificationDto> Items,
    int UnreadCount);

public sealed class ListNotificationsQueryHandler
    : IQueryHandler<ListNotificationsQuery, ListNotificationsResult>
{
    private readonly INotificationRepository _notifications;

    public ListNotificationsQueryHandler(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<ListNotificationsResult> Handle(
        ListNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _notifications.ListByUserAsync(
            request.CurrentUserId,
            request.UnreadOnly,
            request.Page,
            request.PageSize,
            cancellationToken);

        var unreadCount = await _notifications.CountUnreadAsync(
            request.CurrentUserId,
            cancellationToken);

        return new ListNotificationsResult(
            Items: items.Select(NotificationDtoMapper.ToDto).ToList(),
            UnreadCount: unreadCount);
    }
}
