using IsoDocs.Application.Cases;
using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.UnitTests.Fakes;

/// <summary>
/// <see cref="ICaseRelationRepository"/> 的記憶體 fake 實作。
/// <see cref="ParentsWithIncompleteChildren"/> 可在測試中預設，控制 HasIncompleteSubprocessesAsync 的回傳值。
/// </summary>
public sealed class FakeCaseRelationRepository : ICaseRelationRepository
{
    private readonly List<CaseRelation> _store = new();

    public IReadOnlyList<CaseRelation> Store => _store;

    /// <summary>設定哪些父案件被視為「有未完成子流程」（用於測試關閉驗證邏輯）。</summary>
    public HashSet<Guid> ParentsWithIncompleteChildren { get; } = new();

    public void Seed(CaseRelation relation) => _store.Add(relation);

    public Task<IReadOnlyList<CaseRelation>> GetRelationsByCaseIdAsync(
        Guid caseId, CancellationToken cancellationToken = default)
    {
        var result = _store
            .Where(r => r.ParentCaseId == caseId || r.ChildCaseId == caseId)
            .ToList();
        return Task.FromResult<IReadOnlyList<CaseRelation>>(result);
    }

    public Task<bool> HasIncompleteSubprocessesAsync(
        Guid parentCaseId, CancellationToken cancellationToken = default)
        => Task.FromResult(ParentsWithIncompleteChildren.Contains(parentCaseId));

    public Task AddAsync(CaseRelation relation, CancellationToken cancellationToken = default)
    {
        _store.Add(relation);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
