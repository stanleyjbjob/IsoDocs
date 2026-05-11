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

    /// <summary>
    /// 重新啟用已停用的角色。對應 issue #6 [2.2.1] 的 PUT /api/roles/{id}/activate（管理者操作）。
    /// 對系統內建角色為冪等：因為 Deactivate() 已禁止停用，IsActive 永遠為 true，此處不另防呆。
    /// </summary>
    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
