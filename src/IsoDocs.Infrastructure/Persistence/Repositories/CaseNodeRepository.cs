using IsoDocs.Application.Cases;
using IsoDocs.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Persistence.Repositories;

/// <summary>
/// <see cref="ICaseNodeRepository"/> 的 EF Core 實作（issue #30 管理者指派頂替）。
/// </summary>
internal sealed class CaseNodeRepository : ICaseNodeRepository
{
    private readonly IsoDocsDbContext _dbContext;

    public CaseNodeRepository(IsoDocsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CaseNode?> GetByCaseAndNodeAsync(
        Guid caseId, Guid nodeId, CancellationToken cancellationToken = default)
        => await _dbContext.CaseNodes
            .FirstOrDefaultAsync(n => n.CaseId == caseId && n.Id == nodeId, cancellationToken);

    public async Task<IReadOnlyList<CaseNode>> ListActiveByCaseAsync(
        Guid caseId, CancellationToken cancellationToken = default)
        => await _dbContext.CaseNodes
            .AsNoTracking()
            .Where(n => n.CaseId == caseId
                        && (n.Status == CaseNodeStatus.InProgress || n.Status == CaseNodeStatus.Pending))
            .OrderBy(n => n.NodeOrder)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
