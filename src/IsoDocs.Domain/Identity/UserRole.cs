using IsoDocs.Domain.Common;

namespace IsoDocs.Domain.Identity;

/// <summary>
/// 使用者與角色的指派紀錄。一個使用者可同時擁有多個角色（複合角色）。
/// 使用 EffectiveFrom / EffectiveTo 控制有效期間，以利離職、調動或臨時授權。
/// </summary>
public class UserRole : Entity<Guid>
{
    public Guid UserId { get; protected set; }
    public Guid RoleId { get; protected set; }
    public DateTimeOffset EffectiveFrom { get; protected set; }
    public DateTimeOffset? EffectiveTo { get; protected set; }
    public Guid? AssignedByUserId { get; protected set; }
    public DateTimeOffset AssignedAt { get; protected set; } = DateTimeOffset.UtcNow;

    private UserRole() { }

    public UserRole(Guid id, Guid userId, Guid roleId, DateTimeOffset effectiveFrom, DateTimeOffset? effectiveTo, Guid? assignedByUserId)
    {
        Id = id;
        UserId = userId;
        RoleId = roleId;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        AssignedByUserId = assignedByUserId;
    }

    public bool IsEffectiveAt(DateTimeOffset moment) =>
        moment >= EffectiveFrom && (EffectiveTo is null || moment < EffectiveTo);

    public void Revoke(DateTimeOffset at)
    {
        if (EffectiveTo is null || EffectiveTo > at)
            EffectiveTo = at;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
