using IsoDocs.Domain.Common;

namespace IsoDocs.Domain.Identity;

/// <summary>
/// 自訂角色。權限以 JSON 結構儲存於 PermissionsJson，由 Application 層的 Authorization Policy 解析。
/// </summary>
public class Role : Entity<Guid>, IAggregateRoot
{
    public string Name { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }

    /// <summary>
    /// 權限 JSON。預期格式為 string[]，例如 ["case.create", "case.approve", "admin.users.read"]。
    /// </summary>
    public string PermissionsJson { get; protected set; } = "[]";

    public bool IsSystemRole { get; protected set; }
    public bool IsActive { get; protected set; } = true;

    private Role() { }

    public Role(Guid id, string name, string permissionsJson, bool isSystemRole = false)
    {
        Id = id;
        Name = name;
        PermissionsJson = permissionsJson;
        IsSystemRole = isSystemRole;
    }

    public void Update(string name, string? description, string permissionsJson)
    {
        Name = name;
        Description = description;
        PermissionsJson = permissionsJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        if (IsSystemRole)
            throw new DomainException("role.system_role_cannot_deactivate", "系統內建角色不可停用");
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
