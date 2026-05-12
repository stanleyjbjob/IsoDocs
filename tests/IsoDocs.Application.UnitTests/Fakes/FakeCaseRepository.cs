using IsoDocs.Application.Cases;
using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.UnitTests.Fakes;

/// <summary>
/// <see cref="ICaseRepository"/> 的記憶體 fake 實作。不接 DB、不需 EF Core。
/// SaveChangesAsync 是 no-op。測試可透過 SeedCase / SeedNode 預先放入資料。
/// </summary>
public sealed class FakeCaseRepository : ICaseRepository
{
    private readonly Dictionary<Guid, Case> _cases = new();
    private readonly List<CaseNode> _nodes = new();

    public IReadOnlyDictionary<Guid, Case> CaseStore => _cases;
    public IReadOnlyList<CaseNode> NodeStore => _nodes;

    public void SeedCase(Case @case) => _cases[@case.Id] = @case;
    public void SeedNode(CaseNode node) => _nodes.Add(node);

    public Task<Case?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_cases.TryGetValue(id, out var c) ? c : null);

    public Task<CaseNode?> FindActiveNodeAsync(Guid caseId, CancellationToken cancellationToken = default) =>
        Task.FromResult(
            _nodes
                .Where(n => n.CaseId == caseId &&
                           (n.Status == CaseNodeStatus.InProgress || n.Status == CaseNodeStatus.Pending))
                .OrderBy(n => n.NodeOrder)
                .FirstOrDefault());

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
