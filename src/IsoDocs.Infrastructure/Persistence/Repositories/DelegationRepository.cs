using IsoDocs.Application.Identity.Delegations;
using IsoDocs.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Persistence.Repositories;

/// <summary>
/// <see cref="IDelegationRepository"/> 的 EF Core 實作（issue #30 [2.3.2]）。
/// </summary>
internal sealed class DelegationRepository : IDelegationRepository
{
    private readonly IsoDocsDbContext _dbContext;

    public DelegationRepository(IsoDocsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Delegation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _dbContext.Delegations
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Delegation>> ListByDelegatorAsync(
        Guid delegatorUserId, CancellationToken cancellationToken = default)
        => await _dbContext.Delegations
            .AsNoTracking()
            .Where(d => d.DelegatorUserId == delegatorUserId)
            .OrderByDescending(d => d.StartAt)
            .ToListAsync(cancellationToken);

    public async Task<Delegation?> GetEffectiveByDelegatorAsync(
        Guid delegatorUserId, DateTimeOffset moment, CancellationToken cancellationToken = default)
        => await _dbContext.Delegations
            .AsNoTracking()
            .Where(d => d.DelegatorUserId == delegatorUserId
                        && !d.IsRevoked
                        && d.StartAt <= moment
                        && d.EndAt > moment)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(Delegation delegation, CancellationToken cancellationToken = default)
        => await _dbContext.Delegations.AddAsync(delegation, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
