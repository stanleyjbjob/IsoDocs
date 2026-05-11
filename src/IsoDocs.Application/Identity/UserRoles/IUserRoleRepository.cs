using IsoDocs.Domain.Identity;

namespace IsoDocs.Application.Identity.UserRoles;

/// <summary>
/// 使用者角色指派資料存取抽象。
///
/// 一個使用者可同時擁有多個角色（複合角色，issue #6 驗收條件之一）。
/// 每筆 UserRole 有 EffectiveFrom / EffectiveTo 控制生效期間（離職、調動、臨時授權）。
/// </summary>
public interface IUserRoleRepository
{
    /// <summary>列出指定使用者所有的角色指派（含已失效）。</summary>
    Task<IReadOnlyList<UserRole>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出指定使用者在 <paramref name="moment"/> 時點仍生效的角色指派。
    /// 用於 PermissionService 解析當前使用者的有效權限集合。
    /// </summary>
    Task<IReadOnlyList<UserRole>> ListEffectiveByUserIdAsync(
        Guid userId,
        DateTimeOffset moment,
        CancellationToken cancellationToken = default);

    Task AddAsync(UserRole userRole, CancellationToken cancellationToken = default);

    /// <summary>批次新增（指派多個角色時用）。</summary>
    Task AddRangeAsync(IEnumerable<UserRole> userRoles, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
