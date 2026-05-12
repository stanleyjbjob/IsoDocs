using IsoDocs.Application.Cases;
using IsoDocs.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Persistence.Repositories;

/// <summary>
/// <see cref="ICaseRepository"/> 的 EF Core 實作。Scoped 同 DbContext。
/// </summary>
internal sealed class CaseRepository : ICaseRepository
{
    private readonly IsoDocsDbContext _dbContext;

    public CaseRepository(IsoDocsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Case?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Cases.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<CaseNode?> FindActiveNodeAsync(Guid caseId, CancellationToken cancellationToken = default)
        => _dbContext.CaseNodes
            .Where(n => n.CaseId == caseId &&
                       (n.Status == CaseNodeStatus.InProgress || n.Status == CaseNodeStatus.Pending))
            .OrderBy(n => n.NodeOrder)
            .FirstOrDefaultAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
