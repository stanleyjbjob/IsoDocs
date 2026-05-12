using IsoDocs.Application.Cases;
using IsoDocs.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Persistence.Repositories;

internal sealed class CaseRelationRepository : ICaseRelationRepository
{
    private readonly IsoDocsDbContext _dbContext;

    public CaseRelationRepository(IsoDocsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CaseRelation>> ListByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CaseRelations
            .AsNoTracking()
            .Where(r => r.ParentCaseId == caseId || r.ChildCaseId == caseId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(CaseRelation relation, CancellationToken cancellationToken = default)
        => await _dbContext.CaseRelations.AddAsync(relation, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
