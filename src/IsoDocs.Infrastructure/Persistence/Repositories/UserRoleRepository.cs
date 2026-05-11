using IsoDocs.Application.Identity.UserRoles;
using IsoDocs.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Persistence.Repositories;

/// <summary>
/// <see cref="IUserRoleRepository"/> 的 EF Core 實作。
/// </summary>
internal sealed class UserRoleRepository : IUserRoleRepository
{
    private readonly IsoDocsDbContext _dbContext;

    public UserRoleRepository(IsoDocsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UserRole>> ListByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .OrderByDescending(ur => ur.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserRole>> ListEffectiveByUserIdAsync(
        Guid userId, DateTimeOffset moment, CancellationToken cancellationToken = default)
    {
        // 在資料庫端就過濾 EffectiveFrom / EffectiveTo，避免拉全部到記憶體
        return await _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId
                         && ur.EffectiveFrom <= moment
                         && (ur.EffectiveTo == null || ur.EffectiveTo > moment))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserRoles.AddAsync(userRole, cancellationToken);
    }

    public async Task AddRangeAsync(
        IEnumerable<UserRole> userRoles, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserRoles.AddRangeAsync(userRoles, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
