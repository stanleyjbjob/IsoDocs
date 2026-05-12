using IsoDocs.Domain.Communications;

namespace IsoDocs.Application.Communications;

/// <summary>
/// 通知資料存取介面（issue #24 [6.2]）。
/// </summary>
public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> ListByUserAsync(
        Guid recipientUserId,
        bool unreadOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> ListUnreadTrackedAsync(
        Guid recipientUserId,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadAsync(Guid recipientUserId, CancellationToken cancellationToken = default);

    Task<Notification?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
