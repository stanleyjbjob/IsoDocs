using IsoDocs.Domain.Communications;

namespace IsoDocs.Application.Notifications;

/// <summary>
/// <see cref="Notification"/> 的持久化介面。
/// </summary>
public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
