using IsoDocs.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace IsoDocs.Infrastructure.Persistence.Interceptors;

/// <summary>
/// SaveChanges 攜截器：自動為繼承 Entity&lt;T&gt; 的實體設定 CreatedAt / UpdatedAt。
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
            if (entry.Entity is not IHasTimestamps entity) continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    // CreatedAt 由實體預設值貾責，這裡不覆寫
                    break;
                case EntityState.Modified:
                    entity.SetUpdatedAt(now);
                    break;
            }
        }
    }
}

/// <summary>
/// 實體需實作此介面才能被 interceptor 識別。Entity&lt;T&gt; 在 Domain 層以 protected 設定 UpdatedAt，
/// 為避免 Domain 身分泄漏，本介面以顯示衡接。
/// </summary>
public interface IHasTimestamps
{
    void SetUpdatedAt(DateTimeOffset updatedAt);
}
