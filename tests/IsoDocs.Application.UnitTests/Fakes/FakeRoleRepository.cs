using IsoDocs.Application.Identity.Roles;
using IsoDocs.Domain.Identity;

namespace IsoDocs.Application.UnitTests.Fakes;

/// <summary>
/// <see cref="IRoleRepository"/> 的記憶體 fake 實作。不接 DB、不需 EF Core。
/// SaveChangesAsync 是 no-op。測試可透過 <see cref="Seed"/> 預先放入角色。
/// </summary>
public sealed class FakeRoleRepository : IRoleRepository
{
    private readonly Dictionary<Guid, Role> _store = new();

    public IReadOnlyDictionary<Guid, Role> Store => _store;

    /// <summary>預先放一個角色，方便「先 seed 再跑 handler」流程。</summary>
    public Role Seed(string name, string permissionsJson = "[]", bool isSystemRole = false, bool isActive = true)
    {
        var role = new Role(Guid.NewGuid(), name, permissionsJson, isSystemRole);
        role.Update(name, description: null, permissionsJson: permissionsJson);
        if (!isActive)
        {
            // 不能對系統角色停用 (Domain 拋例外)，測試需不能同時設 isSystemRole=true 與 isActive=false
            role.Deactivate();
        }
        _store[role.Id] = role;
        return role;
    }

    public Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Role>>(_store.Values.OrderBy(r => r.Name).ToList());

    public Task<Role?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id, out var role) ? role : null);

    public Task<Role?> FindByNameAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Values.FirstOrDefault(r => r.Name == name));

    public Task<Role?> FindByNameExcludingIdAsync(string name, Guid excludingId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Values.FirstOrDefault(r => r.Name == name && r.Id != excludingId));

    public Task<IReadOnlyList<Role>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Role>>(_store.Values.Where(r => ids.Contains(r.Id)).ToList());

    public Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        _store[role.Id] = role;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
