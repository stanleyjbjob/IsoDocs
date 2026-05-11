using IsoDocs.Application.Cases;
using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.UnitTests.Fakes;

/// <summary>
/// <see cref="IWorkflowRepository"/> 的記憶體 fake 實作。
/// </summary>
public sealed class FakeWorkflowRepository : IWorkflowRepository
{
    private readonly Dictionary<Guid, WorkflowTemplate> _templates = new();
    private readonly List<WorkflowNode> _nodes = new();

    public WorkflowTemplate SeedTemplate(
        string code = "WF01",
        string name = "預設流程",
        bool isActive = true)
    {
        var t = new WorkflowTemplate(Guid.NewGuid(), code, name, Guid.NewGuid());
        _templates[t.Id] = t;
        return t;
    }

    public WorkflowNode SeedNode(
        Guid templateId,
        int templateVersion,
        int order = 1,
        string name = "處理")
    {
        var n = new WorkflowNode(
            Guid.NewGuid(), templateId, templateVersion,
            order, name, WorkflowNodeType.Process, null);
        _nodes.Add(n);
        return n;
    }

    public Task<WorkflowTemplate?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_templates.TryGetValue(id, out var t) ? t : null);

    public Task<IReadOnlyList<WorkflowNode>> ListNodesByTemplateAsync(
        Guid templateId, int templateVersion, CancellationToken cancellationToken = default)
    {
        var nodes = _nodes
            .Where(n => n.WorkflowTemplateId == templateId && n.TemplateVersion == templateVersion)
            .OrderBy(n => n.NodeOrder)
            .ToList();
        return Task.FromResult<IReadOnlyList<WorkflowNode>>(nodes);
    }
}
