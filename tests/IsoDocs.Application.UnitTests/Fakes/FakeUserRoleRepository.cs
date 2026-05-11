using IsoDocs.Application.Identity.UserRoles;
using IsoDocs.Domain.Identity;

namespace IsoDocs.Application.UnitTests.Fakes;

/// <summary>
/// <see cref="IUserRoleRepository"/> 的記憶體 fake 實作。
/// </summary>
public sealed class FakeUserRoleRepository : IUserRoleRepository
{
    private readonly List<UserRole> _store = new();

    public IReadOnlyList<UserRole> Store => _store;

    public Task<IReadOnlyList<UserRole>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<UserRole>>(_store.Where(ur => ur.UserId == userId).ToList());

    public Task<IReadOnlyList<UserRole>> ListEffectiveByUserIdAsync(Guid userId, DateTimeOffset moment, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<UserRole>>(_store.Where(ur => ur.UserId == userId && ur.IsEffectiveAt(moment)).ToList());

    public Task AddAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        _store.Add(userRole);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<UserRole> userRoles, CancellationToken cancellationToken = default)
    {
        _store.AddRange(userRoles);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
