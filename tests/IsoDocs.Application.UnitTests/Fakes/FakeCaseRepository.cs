using IsoDocs.Application.Cases;
using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.UnitTests.Fakes;

/// <summary>
/// <see cref="ICaseRepository"/> 的記憶體 fake 實作。不接 DB，SaveChangesAsync 為 no-op。
/// </summary>
public sealed class FakeCaseRepository : ICaseRepository
{
    private readonly Dictionary<Guid, Case> _store = new();

    public IReadOnlyDictionary<Guid, Case> Store => _store;

    public void Seed(Case caseEntity) => _store[caseEntity.Id] = caseEntity;

    public Task<Case?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.TryGetValue(id, out var c) ? c : null);

    public Task AddAsync(Case caseEntity, CancellationToken cancellationToken = default)
    {
        _store[caseEntity.Id] = caseEntity;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
