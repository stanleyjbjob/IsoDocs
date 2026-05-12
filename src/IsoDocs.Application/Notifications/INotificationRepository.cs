using IsoDocs.Domain.Communications;

namespace IsoDocs.Application.Notifications;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
