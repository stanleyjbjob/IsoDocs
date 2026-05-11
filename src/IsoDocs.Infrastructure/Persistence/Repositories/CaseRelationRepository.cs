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

    /// <summary>回傳此案件作為父或子的所有關聯（雙向）。</summary>
    public async Task<IReadOnlyList<CaseRelation>> GetRelationsByCaseIdAsync(
        Guid caseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CaseRelations
            .AsNoTracking()
            .Where(r => r.ParentCaseId == caseId || r.ChildCaseId == caseId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>主單結案前呼叫：確認是否有子流程仍在進行中。</summary>
    public async Task<bool> HasIncompleteSubprocessesAsync(
        Guid parentCaseId, CancellationToken cancellationToken = default)
    {
        var childIds = await _dbContext.CaseRelations
            .AsNoTracking()
            .Where(r => r.ParentCaseId == parentCaseId && r.RelationType == CaseRelationType.Subprocess)
            .Select(r => r.ChildCaseId)
            .ToListAsync(cancellationToken);

        if (childIds.Count == 0) return false;

        return await _dbContext.Cases
            .AsNoTracking()
            .AnyAsync(c => childIds.Contains(c.Id) && c.Status == CaseStatus.InProgress, cancellationToken);
    }

    public async Task AddAsync(CaseRelation relation, CancellationToken cancellationToken = default)
        => await _dbContext.CaseRelations.AddAsync(relation, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
