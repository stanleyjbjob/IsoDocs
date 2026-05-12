using IsoDocs.Application.Identity.Delegations;
using IsoDocs.Domain.Identity;

namespace IsoDocs.Application.UnitTests.Fakes;

/// <summary>
/// <see cref="IDelegationRepository"/> 的記憶體 fake 實作。
/// </summary>
public sealed class FakeDelegationRepository : IDelegationRepository
{
    private readonly List<Delegation> _store = new();

    public IReadOnlyList<Delegation> Store => _store;

    public Task<Delegation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.FirstOrDefault(d => d.Id == id));

    public Task<IReadOnlyList<Delegation>> ListByDelegatorAsync(
        Guid delegatorUserId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Delegation>>(
            _store.Where(d => d.DelegatorUserId == delegatorUserId).ToList());

    public Task<Delegation?> GetEffectiveByDelegatorAsync(
        Guid delegatorUserId, DateTimeOffset moment, CancellationToken cancellationToken = default)
        => Task.FromResult(
            _store.FirstOrDefault(d => d.DelegatorUserId == delegatorUserId && d.IsEffectiveAt(moment)));

    public Task AddAsync(Delegation delegation, CancellationToken cancellationToken = default)
    {
        _store.Add(delegation);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
