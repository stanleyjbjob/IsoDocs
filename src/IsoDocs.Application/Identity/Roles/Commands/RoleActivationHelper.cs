using IsoDocs.Domain.Identity;

namespace IsoDocs.Application.Identity.Roles.Commands;

/// <summary>
/// 啟用角色的便利方法。歷史包袱（前一輪以反射過渡）已移除：
/// 既然 <see cref="Role.Activate"/> 已是 public 方法，直接呼叫即可。
/// 保留 helper 名稱以維持既有 caller（SetRoleActiveCommandHandler）不變。
/// </summary>
internal static class RoleActivationHelper
{
    public static void Activate(Role role) => role.Activate();
}
