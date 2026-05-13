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

    public Case Seed(string caseNumber = "ITCT-F01-260001", string title = "測試案件")
    {
        var @case = new Case(
            Guid.NewGuid(), caseNumber, title,
            Guid.NewGuid(), Guid.NewGuid(), 1, 1,
            Guid.NewGuid(), null, null);
        _store[@case.Id] = @case;
        return @case;
    }

    public Task<Case?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id, out var c) ? c : null);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
