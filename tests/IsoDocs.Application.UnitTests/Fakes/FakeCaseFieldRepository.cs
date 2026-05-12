using IsoDocs.Application.Cases.FieldInheritance;
using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.UnitTests.Fakes;

/// <summary>
/// <see cref="ICaseFieldRepository"/> 的記憶體 fake 實作。不接 DB。
/// </summary>
public sealed class FakeCaseFieldRepository : ICaseFieldRepository
{
    private readonly List<CaseField> _store = new();

    public IReadOnlyList<CaseField> Store => _store;

    public CaseField Seed(Guid caseId, Guid fieldDefinitionId, string fieldCode, string valueJson = "\"value\"", int fieldVersion = 1)
    {
        var field = new CaseField(Guid.NewGuid(), caseId, fieldDefinitionId, fieldVersion, fieldCode, valueJson);
        _store.Add(field);
        return field;
    }

    public Task<IReadOnlyList<CaseField>> ListByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CaseField> result = _store.Where(f => f.CaseId == caseId).ToList();
        return Task.FromResult(result);
    }

    public Task AddRangeAsync(IReadOnlyList<CaseField> fields, CancellationToken cancellationToken = default)
    {
        _store.AddRange(fields);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
