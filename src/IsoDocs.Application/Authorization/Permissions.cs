namespace IsoDocs.Application.Authorization;

/// <summary>
/// 系統權限碼常數。Role.PermissionsJson 應只能包含這些字串。
///
/// 命名規則：&lt;resource&gt;.&lt;action&gt;，小寫底線。新增權限時：
///   1. 在這裡加上常數。
///   2. 同步更新 <see cref="All"/>。
///   3. 若需要新的 Authorization Policy，於 Program.cs 內 AddAuthorization 註冊。
///
/// 後續 issue（#7~#28）會持續擴充這份清單。本檔目前涵蓋 issue #6 [2.2.1] RBAC 必需的權限。
/// </summary>
public static class Permissions
{
    // ── 角色管理（RBAC 自身）─────────────────────────────────────────
    /// <summary>讀取角色清單與權限定義。</summary>
    public const string RolesRead = "roles.read";

    /// <summary>建立、修改、停用角色。</summary>
    public const string RolesWrite = "roles.write";

    // ── 使用者管理 ────────────────────────────────────────────────
    /// <summary>讀取使用者清單與所屬角色。</summary>
    public const string UsersRead = "users.read";

    /// <summary>指派或撤銷使用者角色（複合角色）。</summary>
    public const string UsersAssignRoles = "users.assign_roles";

    /// <summary>邀請新使用者（透過 Microsoft Graph，issue #3）。</summary>
    public const string UsersInvite = "users.invite";

    // ── 系統管理（萬用旁路）─────────────────────────────────────────
    /// <summary>系統管理員萬用權限。User.IsSystemAdmin=true 視同擁有此權限。</summary>
    public const string AdminFullAccess = "admin.full_access";

    /// <summary>本檔目前已知全部權限碼。Validator 用它檢查 PermissionsJson 是否合法。</summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        RolesRead,
        RolesWrite,
        UsersRead,
        UsersAssignRoles,
        UsersInvite,
        AdminFullAccess
    };

    /// <summary>系統內建管理者角色預設帶的權限碼。</summary>
    public static readonly IReadOnlyList<string> SystemAdminDefaults = new[]
    {
        AdminFullAccess,
        RolesRead,
        RolesWrite,
        UsersRead,
        UsersAssignRoles,
        UsersInvite
    };

    /// <summary>判斷字串是否為已知的權限碼。</summary>
    public static bool IsKnown(string permission) =>
        !string.IsNullOrWhiteSpace(permission) && All.Contains(permission, StringComparer.Ordinal);
}
