using IsoDocs.Application.Cases;
using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.UnitTests.Fakes;

/// <summary>
/// <see cref="IDocumentTypeRepository"/> 的記憶體 fake 實作。
/// </summary>
public sealed class FakeDocumentTypeRepository : IDocumentTypeRepository
{
    private readonly Dictionary<Guid, DocumentType> _store = new();

    public DocumentType Seed(
        string companyCode = "ITCT",
        string code = "F01",
        string name = "工作需求單")
    {
        var dt = new DocumentType(Guid.NewGuid(), companyCode, code, name, DateTimeOffset.UtcNow.Year);
        _store[dt.Id] = dt;
        return dt;
    }

    public Task<DocumentType?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id, out var dt) ? dt : null);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
