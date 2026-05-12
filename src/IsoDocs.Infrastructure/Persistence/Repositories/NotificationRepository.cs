using IsoDocs.Application.Notifications;
using IsoDocs.Domain.Communications;

namespace IsoDocs.Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly IsoDocsDbContext _db;

    public NotificationRepository(IsoDocsDbContext db) => _db = db;

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
        => await _db.Notifications.AddAsync(notification, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}
