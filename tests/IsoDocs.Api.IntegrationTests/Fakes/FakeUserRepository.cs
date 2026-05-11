using IsoDocs.Application.Users;
using IsoDocs.Domain.Identity;

namespace IsoDocs.Api.IntegrationTests.Fakes;

public sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = new();
    private readonly List<Role> _roles = new();
    private readonly List<UserRole> _userRoles = new();

    public IReadOnlyList<User> Users => _users.AsReadOnly();
    public IReadOnlyList<UserRole> UserRoles => _userRoles.AsReadOnly();

    public User SeedUser(string oid, string email, string displayName, bool isSystemAdmin = false)
    {
        var user = new User(Guid.NewGuid(), oid, email, displayName);
        user.UpdateProfile(email, displayName, null, null);
        if (isSystemAdmin)
            user.MakeSystemAdmin();
        _users.Add(user);
        return user;
    }

    public Role SeedRole(string name)
    {
        var role = new Role(Guid.NewGuid(), name, "[]");
        _roles.Add(role);
        return role;
    }

    public Task<User?> FindByAzureAdObjectIdAsync(string oid, CancellationToken ct = default)
        => Task.FromResult(_users.FirstOrDefault(u => u.AzureAdObjectId == oid));

    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
        => Task.FromResult(_users.FirstOrDefault(u => u.Email == email && u.IsActive));

    public Task<Role?> FindRoleByIdAsync(Guid roleId, CancellationToken ct = default)
        => Task.FromResult(_roles.FirstOrDefault(r => r.Id == roleId && r.IsActive));

    public Task AddUserAsync(User user, CancellationToken ct = default)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task AddUserRoleAsync(UserRole userRole, CancellationToken ct = default)
    {
        _userRoles.Add(userRole);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}
