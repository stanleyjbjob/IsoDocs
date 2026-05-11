using IsoDocs.Application.Users;
using IsoDocs.Domain.Identity;
using IsoDocs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly IsoDocsDbContext _dbContext;

    public UserRepository(IsoDocsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> FindByAzureAdObjectIdAsync(string oid, CancellationToken ct = default)
        => _dbContext.Users.FirstOrDefaultAsync(u => u.AzureAdObjectId == oid, ct);

    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
        => _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);

    public Task<Role?> FindRoleByIdAsync(Guid roleId, CancellationToken ct = default)
        => _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == roleId && r.IsActive, ct);

    public async Task AddUserAsync(User user, CancellationToken ct = default)
        => await _dbContext.Users.AddAsync(user, ct);

    public async Task AddUserRoleAsync(UserRole userRole, CancellationToken ct = default)
        => await _dbContext.UserRoles.AddAsync(userRole, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _dbContext.SaveChangesAsync(ct);
}
