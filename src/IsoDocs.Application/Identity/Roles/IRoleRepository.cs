using IsoDocs.Domain.Identity;

namespace IsoDocs.Application.Identity.Roles;

/// <summary>
/// 角色資料存取抽象。Infrastructure 層以 EF Core 實作；測試以 in-memory fake 實作。
///
/// 範圍只放與「角色聚合根」相關的 CRUD；角色與使用者的關聯由 <see cref="UserRoles.IUserRoleRepository"/> 負責。
/// </summary>
public interface IRoleRepository
{
    /// <summary>列出全部角色（含停用）。前端清單頁可自行過濾。</summary>
    Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>依 Id 取得角色，找不到回傳 null。</summary>
    Task<Role?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>依名稱（精確比對）查找角色，用來偵測 NameDuplicate。</summary>
    Task<Role?> FindByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>依名稱比對排除指定 Id（UPDATE 時用，避免比對到自己）。</summary>
    Task<Role?> FindByNameExcludingIdAsync(string name, Guid excludingId, CancellationToken cancellationToken = default);

    /// <summary>批次依 Id 取得角色（指派時用，效能優於 N 次 FindByIdAsync）。</summary>
    Task<IReadOnlyList<Role>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    Task AddAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>持久化異動。對應 DbContext.SaveChangesAsync()。</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
