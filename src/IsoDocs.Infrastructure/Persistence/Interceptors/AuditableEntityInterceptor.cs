using IsoDocs.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace IsoDocs.Infrastructure.Persistence.Interceptors;

/// <summary>
/// SaveChanges 攜截器：自動為繼承 Entity&lt;T&gt; 的實體設定 UpdatedAt。
/// CreatedAt 由實體預設值貾責。
/// </summary>
public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateTimestamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateTimestamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateTimestamps(DbContext? context)
    {
        if (context is null) return;

        var now = DateTimeOffset.UtcNow;
        foreach (EntityEntry entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IHasTimestamps timestamped) continue;

            if (entry.State == EntityState.Modified)
            {
                timestamped.SetUpdatedAt(now);
            }
        }
    }
}
