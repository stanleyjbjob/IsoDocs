using IsoDocs.Application.Cases;
using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.UnitTests.Fakes;

/// <summary>
/// <see cref="ICaseRepository"/> 的記憶體 fake 實作。不接 DB、不需 EF Core。
/// </summary>
public sealed class FakeCaseRepository : ICaseRepository
{
    private readonly Dictionary<Guid, Case> _store = new();

    public IReadOnlyDictionary<Guid, Case> Store => _store;

    public Case Seed(
        string caseNumber,
        string title,
        Guid documentTypeId,
        Guid workflowTemplateId,
        Guid initiatedByUserId,
        CaseStatus status = CaseStatus.InProgress)
    {
        var c = new Case(
            id: Guid.NewGuid(),
            caseNumber: caseNumber,
            title: title,
            documentTypeId: documentTypeId,
            workflowTemplateId: workflowTemplateId,
            templateVersion: 1,
            fieldVersion: 1,
            initiatedByUserId: initiatedByUserId,
            expectedCompletionAt: null,
            customerId: null);

        if (status == CaseStatus.Closed)
            c.Close();
        else if (status == CaseStatus.Voided)
            c.Void();

        _store[c.Id] = c;
        return c;
    }

    public Task<Case?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id, out var c) ? c : null);

    public Task AddAsync(Case caseEntity, CancellationToken cancellationToken = default)
    {
        _store[caseEntity.Id] = caseEntity;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
