using System.Reflection;
using IsoDocs.Domain.Identity;

namespace IsoDocs.Application.Identity.Roles.Commands;

/// <summary>
/// 過渡用 helper：Role.Activate() 尚未存在於 Domain 時，以反射切換 IsActive 並維護 UpdatedAt。
/// 後續 commit 在 Domain.Role 上新增公開 Activate() 後，本檔可移除並直接呼叫 role.Activate()。
///
/// 設計理由：避免「先動 Domain → 影響其他 PR review 時 conflict」的耦合，先讓 Application 層獨立可動。
/// </summary>
internal static class RoleActivationHelper
{
    private static readonly PropertyInfo? IsActiveProp = typeof(Role).GetProperty(
        "IsActive", BindingFlags.Instance | BindingFlags.Public);
    private static readonly PropertyInfo? UpdatedAtProp = typeof(Role).GetProperty(
        "UpdatedAt", BindingFlags.Instance | BindingFlags.Public);

    public static void Activate(Role role)
    {
        if (role.IsActive)
        {
            return;
        }

        // 優先呼叫公開 Activate()（後續 commit 補上後自然走這條路徑）
        var activateMethod = typeof(Role).GetMethod(
            "Activate", BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes);
        if (activateMethod is not null)
        {
            activateMethod.Invoke(role, null);
            return;
        }

        // Fallback：反射寫 IsActive 與 UpdatedAt（兩個 setter 是 protected）
        IsActiveProp?.GetSetMethod(nonPublic: true)?.Invoke(role, new object?[] { true });
        UpdatedAtProp?.GetSetMethod(nonPublic: true)?.Invoke(role, new object?[] { DateTimeOffset.UtcNow });
    }
}
