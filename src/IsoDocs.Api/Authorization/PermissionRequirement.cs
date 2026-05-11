using Microsoft.AspNetCore.Authorization;

namespace IsoDocs.Api.Authorization;

/// <summary>
/// ASP.NET Core Authorization 的 Requirement。一個 Requirement 對應一個權限碼，由
/// <see cref="PermissionAuthorizationHandler"/> 負責驗證。
///
/// 使用方式（在 Controller / Action 上）：
/// <code>
/// [Authorize(Policy = Permissions.RolesWrite)]
/// public IActionResult CreateRole(...) { ... }
/// </code>
///
/// Policy 名稱 = 權限碼（例如 <c>roles.write</c>），透過
/// <see cref="AuthorizationPoliciesExtensions.AddIsoDocsPermissionPolicies"/> 一次性註冊。
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
