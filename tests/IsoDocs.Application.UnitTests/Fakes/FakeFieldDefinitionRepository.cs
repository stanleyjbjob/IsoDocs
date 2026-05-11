using IsoDocs.Application.FieldDefinitions;
using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.UnitTests.Fakes;

/// <summary>
/// <see cref="IFieldDefinitionRepository"/> 的記憶體 fake 實作。不接 DB、不需 EF Core。
/// SaveChangesAsync 是 no-op。測試可透過 <see cref="Seed"/> 預先放入欄位定義。
/// </summary>
public sealed class FakeFieldDefinitionRepository : IFieldDefinitionRepository
{
    private readonly Dictionary<Guid, FieldDefinition> _store = new();

    public IReadOnlyDictionary<Guid, FieldDefinition> Store => _store;

    public FieldDefinition Seed(
        string code, string name,
        FieldType type = FieldType.Text, bool isRequired = false,
        string? validationJson = null, string? optionsJson = null)
    {
        var fd = new FieldDefinition(Guid.NewGuid(), code, name, type, isRequired, validationJson, optionsJson);
        _store[fd.Id] = fd;
        return fd;
    }

    public Task<IReadOnlyList<FieldDefinition>> ListAsync(
        bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var list = _store.Values
            .Where(f => includeInactive || f.IsActive)
            .OrderBy(f => f.Code)
            .ToList();
        return Task.FromResult<IReadOnlyList<FieldDefinition>>(list);
    }

    public Task<FieldDefinition?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id, out var fd) ? fd : null);

    public Task<FieldDefinition?> FindByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Values.FirstOrDefault(f => f.Code == code));

    public Task AddAsync(FieldDefinition fieldDefinition, CancellationToken cancellationToken = default)
    {
        _store[fieldDefinition.Id] = fieldDefinition;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
