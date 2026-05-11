using IsoDocs.Domain.Identity;

namespace IsoDocs.Application.Users;

public interface IUserRepository
{
    Task<User?> FindByAzureAdObjectIdAsync(string oid, CancellationToken ct = default);
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<Role?> FindRoleByIdAsync(Guid roleId, CancellationToken ct = default);
    Task AddUserAsync(User user, CancellationToken ct = default);
    Task AddUserRoleAsync(UserRole userRole, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
