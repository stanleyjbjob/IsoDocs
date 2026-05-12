using IsoDocs.Application.Cases;
using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.UnitTests.Fakes;

/// <summary>
/// <see cref="ICaseRepository"/> 的記憶體 fake 實作。不接 DB、不需 EF Core。
/// </summary>
public sealed class FakeCaseRepository : ICaseRepository
{
    private readonly Dictionary<Guid, Case> _cases = new();
    private readonly List<CaseRelation> _relations = new();
    public readonly List<CaseAction> Actions = new();

    public void SeedCase(Case @case) => _cases[@case.Id] = @case;

    public void SeedRelation(CaseRelation relation) => _relations.Add(relation);

    public Task<Case?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_cases.TryGetValue(id, out var c) ? c : null);

    public Task<IReadOnlyList<Case>> FindSubprocessChildrenAsync(Guid parentCaseId, CancellationToken cancellationToken = default)
    {
        var childIds = _relations
            .Where(r => r.ParentCaseId == parentCaseId && r.RelationType == CaseRelationType.Subprocess)
            .Select(r => r.ChildCaseId)
            .ToHashSet();

        IReadOnlyList<Case> children = _cases.Values
            .Where(c => childIds.Contains(c.Id))
            .ToList();

        return Task.FromResult(children);
    }

    public Task AddCaseActionAsync(CaseAction action, CancellationToken cancellationToken = default)
    {
        Actions.Add(action);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>快速建立並 seed 一個進行中的案件。</summary>
    public Case CreateInProgressCase(Guid? id = null)
    {
        var caseId = id ?? Guid.NewGuid();
        var c = new Case(
            id: caseId,
            caseNumber: $"TEST-{caseId:N}",
            title: "測試案件",
            documentTypeId: Guid.NewGuid(),
            workflowTemplateId: Guid.NewGuid(),
            templateVersion: 1,
            fieldVersion: 1,
            initiatedByUserId: Guid.NewGuid(),
            expectedCompletionAt: null,
            customerId: null);
        SeedCase(c);
        return c;
    }
}
