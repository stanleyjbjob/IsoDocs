namespace IsoDocs.Application.Identity;

/// <summary>
/// 角色與 RBAC 相關錯誤碼。對齊 issue #2 的 AuthErrorCodes 形式（前綴/類別碼）以便
/// GlobalExceptionMiddleware 與前端統一處理。
/// </summary>
public static class RoleErrorCodes
{
    public const string Prefix = "ROLE";

    /// <summary>找不到對應角色（HTTP 404）。</summary>
    public const string NotFound = Prefix + "/NOT_FOUND";

    /// <summary>同名角色已存在（HTTP 409）。</summary>
    public const string NameDuplicate = Prefix + "/NAME_DUPLICATE";

    /// <summary>嘗試對系統內建角色執行不允許的動作，例如停用（HTTP 409）。</summary>
    public const string SystemRoleImmutable = Prefix + "/SYSTEM_ROLE_IMMUTABLE";

    /// <summary>PermissionsJson 內含未知或未授權的權限碼（HTTP 422）。</summary>
    public const string InvalidPermissions = Prefix + "/INVALID_PERMISSIONS";

    /// <summary>指派角色失敗（例如指派給已停用使用者，或角色不存在）。</summary>
    public const string AssignmentFailed = Prefix + "/ASSIGNMENT_FAILED";
}
