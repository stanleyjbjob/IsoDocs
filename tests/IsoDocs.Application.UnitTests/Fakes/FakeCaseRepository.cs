using IsoDocs.Application.Cases;
using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.UnitTests.Fakes;

/// <summary>
/// <see cref="ICaseRepository"/> 的記憶體 fake 實作。不接 DB，SaveChangesAsync 是 no-op。
/// </summary>
public sealed class FakeCaseRepository : ICaseRepository
{
    private readonly Dictionary<Guid, Case> _cases = new();
    private readonly Dictionary<Guid, CaseNode> _nodes = new();
    private readonly List<CaseAction> _actions = new();

    public IReadOnlyList<CaseAction> Actions => _actions;

    public void SeedCase(Case @case) => _cases[@case.Id] = @case;
    public void SeedNode(CaseNode node) => _nodes[node.Id] = node;

    public Task<Case?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_cases.TryGetValue(id, out var c) ? c : null);

    public Task<CaseNode?> FindNodeByIdAsync(Guid nodeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_nodes.TryGetValue(nodeId, out var n) ? n : null);

    public Task<IReadOnlyList<SignOffEntryDto>> GetSignOffTrailAsync(
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        var trail = _actions
            .Where(a => a.CaseId == caseId && a.ActionType == CaseActionType.SignOff)
            .OrderBy(a => a.ActionAt)
            .Select(a =>
            {
                var nodeName = a.CaseNodeId.HasValue && _nodes.TryGetValue(a.CaseNodeId.Value, out var n)
                    ? n.NodeName
                    : null;
                return new SignOffEntryDto(
                    Id: a.Id,
                    CaseId: a.CaseId,
                    CaseNodeId: a.CaseNodeId,
                    NodeName: nodeName,
                    ActorUserId: a.ActorUserId,
                    Comment: a.Comment,
                    ActionAt: a.ActionAt);
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<SignOffEntryDto>>(trail);
    }

    public Task AddActionAsync(CaseAction action, CancellationToken cancellationToken = default)
    {
        _actions.Add(action);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
