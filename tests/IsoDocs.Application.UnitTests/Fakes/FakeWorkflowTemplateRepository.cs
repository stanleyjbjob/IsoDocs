using IsoDocs.Application.Workflows;
using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.UnitTests.Fakes;

/// <summary>
/// <see cref="IWorkflowTemplateRepository"/> 的記憶體 fake 實作。不接 DB、不需 EF Core。
/// </summary>
public sealed class FakeWorkflowTemplateRepository : IWorkflowTemplateRepository
{
    private readonly Dictionary<Guid, WorkflowTemplate> _templates = new();
    private readonly List<WorkflowNode> _nodes = new();

    public WorkflowTemplate Seed(string code, string name, Guid? createdBy = null)
    {
        var t = new WorkflowTemplate(Guid.NewGuid(), code, name, createdBy ?? Guid.NewGuid());
        _templates[t.Id] = t;
        return t;
    }

    public Task<IReadOnlyList<WorkflowTemplate>> ListAsync(bool includeInactive = true, CancellationToken cancellationToken = default)
    {
        var result = includeInactive
            ? _templates.Values.ToList()
            : _templates.Values.Where(t => t.IsActive).ToList();
        return Task.FromResult<IReadOnlyList<WorkflowTemplate>>(result);
    }

    public Task<WorkflowTemplate?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_templates.TryGetValue(id, out var t) ? t : null);

    public Task<WorkflowTemplate?> FindByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_templates.Values.FirstOrDefault(t => t.Code == code));

    public Task<WorkflowTemplate?> FindByCodeExcludingIdAsync(string code, Guid excludingId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_templates.Values.FirstOrDefault(t => t.Code == code && t.Id != excludingId));

    public Task<IReadOnlyList<WorkflowNode>> ListNodesAsync(Guid templateId, int version, CancellationToken cancellationToken = default)
    {
        var result = _nodes
            .Where(n => n.WorkflowTemplateId == templateId && n.TemplateVersion == version)
            .OrderBy(n => n.NodeOrder)
            .ToList();
        return Task.FromResult<IReadOnlyList<WorkflowNode>>(result);
    }

    public Task AddAsync(WorkflowTemplate template, CancellationToken cancellationToken = default)
    {
        _templates[template.Id] = template;
        return Task.CompletedTask;
    }

    public Task AddNodesAsync(IEnumerable<WorkflowNode> nodes, CancellationToken cancellationToken = default)
    {
        _nodes.AddRange(nodes);
        return Task.CompletedTask;
    }

    public Task RemoveNodesAsync(Guid templateId, int version, CancellationToken cancellationToken = default)
    {
        _nodes.RemoveAll(n => n.WorkflowTemplateId == templateId && n.TemplateVersion == version);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
