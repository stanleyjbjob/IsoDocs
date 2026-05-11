using Microsoft.AspNetCore.Authorization;

namespace IsoDocs.Application.Authorization;

/// <summary>
/// ASP.NET Core Authorization 的 Requirement。一個 Requirement 對應一個權限碼，由
/// PermissionAuthorizationHandler（在 Infrastructure 層）負責驗證。
///
/// 使用方式（在 Controller / Action 上）：
/// <code>
/// [Authorize(Policy = Permissions.RolesWrite)]
/// public IActionResult CreateRole(...) { ... }
/// </code>
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("permission 不可為空", nameof(permission));
        Permission = permission;
    }

    /// <summary>所需的權限碼，例如 <c>roles.write</c>。</summary>
    public string Permission { get; }
}
