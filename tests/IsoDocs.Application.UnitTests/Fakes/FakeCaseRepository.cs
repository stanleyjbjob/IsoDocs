using IsoDocs.Application.Cases;
using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.UnitTests.Fakes;

/// <summary>
/// <see cref="ICaseRepository"/> 的記憶體 fake 實作。不接 DB、不需 EF Core。
/// </summary>
public sealed class FakeCaseRepository : ICaseRepository
{
    private readonly Dictionary<Guid, Case> _cases = new();
    private readonly List<CaseNode> _nodes = new();

    public IReadOnlyDictionary<Guid, Case> CaseStore => _cases;
    public IReadOnlyList<CaseNode> NodeStore => _nodes;

    public Task<Case?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_cases.TryGetValue(id, out var c) ? c : null);

    public Task<CaseNode?> FindActiveNodeAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var node = _nodes
            .Where(n => n.CaseId == caseId &&
                (n.Status == CaseNodeStatus.Pending || n.Status == CaseNodeStatus.InProgress))
            .OrderBy(n => n.NodeOrder)
            .FirstOrDefault();
        return Task.FromResult(node);
    }

    public Task AddAsync(Case @case, CancellationToken cancellationToken = default)
    {
        _cases[@case.Id] = @case;
        return Task.CompletedTask;
    }

    public Task AddNodeAsync(CaseNode node, CancellationToken cancellationToken = default)
    {
        _nodes.Add(node);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
