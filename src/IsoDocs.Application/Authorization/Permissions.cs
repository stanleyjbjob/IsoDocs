namespace IsoDocs.Application.Authorization;

public static class Permissions
{
    // ── 角色管理（RBAC 自身）
    public const string RolesRead = "roles.read";
    public const string RolesWrite = "roles.write";

    // ── 使用者管理
    public const string UsersRead = "users.read";
    public const string UsersAssignRoles = "users.assign_roles";
    public const string UsersInvite = "users.invite";

    // ── 客戶管理（issue #14 [4.1]）
    public const string CustomersRead = "customers.read";
    public const string CustomersWrite = "customers.write";

    // ── 系統管理（萬用旁路）
    public const string AdminFullAccess = "admin.full_access";

    public static readonly IReadOnlyList<string> All = new[]
    {
        RolesRead,
        RolesWrite,
        UsersRead,
        UsersAssignRoles,
        UsersInvite,
        CustomersRead,
        CustomersWrite,
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
        CustomersRead,
        CustomersWrite
    };

    public static bool IsKnown(string permission) =>
        !string.IsNullOrWhiteSpace(permission) && All.Contains(permission, StringComparer.Ordinal);
}
