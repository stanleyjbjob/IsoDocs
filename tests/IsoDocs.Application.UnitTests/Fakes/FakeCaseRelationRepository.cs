using IsoDocs.Application.Cases;
using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.UnitTests.Fakes;

/// <summary>
/// <see cref="ICaseRelationRepository"/> 的記憶體 fake 實作。
/// </summary>
public sealed class FakeCaseRelationRepository : ICaseRelationRepository
{
    private readonly List<CaseRelation> _store = new();

    public IReadOnlyList<CaseRelation> Store => _store;

    public Task<IReadOnlyList<CaseRelation>> ListByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var result = _store
            .Where(r => r.ParentCaseId == caseId || r.ChildCaseId == caseId)
            .ToList();
        return Task.FromResult<IReadOnlyList<CaseRelation>>(result);
    }

    public Task AddAsync(CaseRelation relation, CancellationToken cancellationToken = default)
    {
        _store.Add(relation);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
