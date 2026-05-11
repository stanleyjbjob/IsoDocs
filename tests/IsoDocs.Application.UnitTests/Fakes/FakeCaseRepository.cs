using IsoDocs.Application.Cases;
using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.UnitTests.Fakes;

/// <summary>
/// <see cref="ICaseRepository"/> 的記憶體 fake 實作。不接 DB、不需 EF Core。
/// </summary>
public sealed class FakeCaseRepository : ICaseRepository
{
    private readonly Dictionary<Guid, Case> _cases = new();
    private readonly Dictionary<Guid, List<CaseNode>> _nodes = new();
    private readonly List<CaseAction> _actions = new();

    public IReadOnlyList<CaseAction> Actions => _actions;

    public Case SeedCase(Case @case)
    {
        _cases[@case.Id] = @case;
        if (!_nodes.ContainsKey(@case.Id))
            _nodes[@case.Id] = new List<CaseNode>();
        return @case;
    }

    public CaseNode SeedNode(Guid caseId, CaseNode node)
    {
        if (!_nodes.ContainsKey(caseId))
            _nodes[caseId] = new List<CaseNode>();
        _nodes[caseId].Add(node);
        return node;
    }

    public Task<Case?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_cases.TryGetValue(id, out var c) ? c : null);

    public Task<IReadOnlyList<CaseNode>> GetNodesAsync(Guid caseId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CaseNode>>(
            _nodes.TryGetValue(caseId, out var nodes)
                ? nodes.OrderBy(n => n.NodeOrder).ToList()
                : new List<CaseNode>());

    public Task AddActionAsync(CaseAction action, CancellationToken cancellationToken = default)
    {
        _actions.Add(action);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
