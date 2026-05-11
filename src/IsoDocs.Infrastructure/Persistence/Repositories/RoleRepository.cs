using IsoDocs.Application.Identity.Roles;
using IsoDocs.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Persistence.Repositories;

/// <summary>
/// <see cref="IRoleRepository"/> 的 EF Core 實作。Scoped 同 DbContext。
/// </summary>
internal sealed class RoleRepository : IRoleRepository
{
    private readonly IsoDocsDbContext _dbContext;

    public RoleRepository(IsoDocsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Role?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public Task<Role?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }

    public Task<Role?> FindByNameExcludingIdAsync(
        string name, Guid excludingId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == name && r.Id != excludingId, cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> ListByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0) return Array.Empty<Role>();
        return await _dbContext.Roles
            .Where(r => ids.Contains(r.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _dbContext.Roles.AddAsync(role, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
