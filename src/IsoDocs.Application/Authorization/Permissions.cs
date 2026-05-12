namespace IsoDocs.Application.Authorization;

/// <summary>
/// 系統權限碼常數。Role.PermissionsJson 應只能包含這些字串。
///
/// 命名規則：&lt;resource&gt;.&lt;action&gt;，小寫底線。新增權限時：
///   1. 在這裡加上常數。
///   2. 同步更新 <see cref="All"/>。
///   3. 若需要新的 Authorization Policy，於 Program.cs 內 AddAuthorization 註冊。
/// </summary>
public static class Permissions
{
    // ── 角色管理（RBAC 自身）─────────────────────────────────────────
    public const string RolesRead = "roles.read";
    public const string RolesWrite = "roles.write";

    // ── 使用者管理 ────────────────────────────────────────────────
    public const string UsersRead = "users.read";
    public const string UsersAssignRoles = "users.assign_roles";
    public const string UsersInvite = "users.invite";

    // ── 附件管理（issue #26 [7.2]）───────────────────────────────────
    /// <summary>讀取案件附件清單與下載 URL。</summary>
    public const string AttachmentsRead = "attachments.read";

    /// <summary>上傳附件（取得 SAS 上傳 URL）。</summary>
    public const string AttachmentsWrite = "attachments.write";

    // ── 系統管理（萬用旁路）─────────────────────────────────────────
    public const string AdminFullAccess = "admin.full_access";

    public static readonly IReadOnlyList<string> All = new[]
    {
        RolesRead,
        RolesWrite,
        UsersRead,
        UsersAssignRoles,
        UsersInvite,
        AttachmentsRead,
        AttachmentsWrite,
        AdminFullAccess
    };

    public static readonly IReadOnlyList<string> SystemAdminDefaults = new[]
    {
        AdminFullAccess,
        RolesRead,
        RolesWrite,
        UsersRead,
        UsersAssignRoles,
        UsersInvite,
        AttachmentsRead,
        AttachmentsWrite
    };

    public static bool IsKnown(string permission) =>
        !string.IsNullOrWhiteSpace(permission) && All.Contains(permission, StringComparer.Ordinal);
}
