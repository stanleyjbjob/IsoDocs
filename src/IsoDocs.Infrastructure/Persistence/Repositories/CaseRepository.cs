using IsoDocs.Application.Cases;
using IsoDocs.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Persistence.Repositories;

public sealed class CaseRepository : ICaseRepository
{
    private readonly IsoDocsDbContext _db;

    public CaseRepository(IsoDocsDbContext db)
    {
        _db = db;
    }

    public Task<Case?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Cases.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Case>> FindSubprocessChildrenAsync(Guid parentCaseId, CancellationToken cancellationToken = default)
    {
        var childIds = await _db.CaseRelations
            .Where(r => r.ParentCaseId == parentCaseId && r.RelationType == CaseRelationType.Subprocess)
            .Select(r => r.ChildCaseId)
            .ToListAsync(cancellationToken);

        if (childIds.Count == 0)
            return Array.Empty<Case>();

        return await _db.Cases
            .Where(c => childIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddCaseActionAsync(CaseAction action, CancellationToken cancellationToken = default)
    {
        await _db.CaseActions.AddAsync(action, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
