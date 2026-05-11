using IsoDocs.Domain.Common;

namespace IsoDocs.Domain.Identity;

/// <summary>
/// 系統使用者。對應 Azure AD / Entra ID 中的帳號。
/// 離職人員於 Azure AD 失效時，本系統將同步停用（IsActive=false）。
/// </summary>
public class User : Entity<Guid>, IAggregateRoot
{
    public string AzureAdObjectId { get; protected set; } = string.Empty;
    public string Email { get; protected set; } = string.Empty;
    public string DisplayName { get; protected set; } = string.Empty;
    public string? Department { get; protected set; }
    public string? JobTitle { get; protected set; }
    public bool IsActive { get; protected set; } = true;

    /// <summary>系統管理者標記，獨立於角色之外，方便權限種子化。</summary>
    public bool IsSystemAdmin { get; protected set; }

    private User() { }

    public User(Guid id, string azureAdObjectId, string email, string displayName)
    {
        Id = id;
        AzureAdObjectId = azureAdObjectId;
        Email = email;
        DisplayName = displayName;
    }

    public void UpdateProfile(string email, string displayName, string? department, string? jobTitle)
    {
        Email = email;
        DisplayName = displayName;
        Department = department;
        JobTitle = jobTitle;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MakeSystemAdmin()
    {
        IsSystemAdmin = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
