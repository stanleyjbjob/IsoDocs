using IsoDocs.Application.Common.Messaging;

namespace IsoDocs.Application.Workflows.Queries;

/// <summary>
/// 列出流程範本清單。對應 GET /api/workflow-templates。
/// </summary>
public sealed record ListWorkflowTemplatesQuery(bool IncludeInactive = true) : IQuery<IReadOnlyList<WorkflowTemplateSummaryDto>>;

public sealed class ListWorkflowTemplatesQueryHandler : IQueryHandler<ListWorkflowTemplatesQuery, IReadOnlyList<WorkflowTemplateSummaryDto>>
{
    private readonly IWorkflowTemplateRepository _templates;

    public ListWorkflowTemplatesQueryHandler(IWorkflowTemplateRepository templates)
    {
        _templates = templates;
    }

    public async Task<IReadOnlyList<WorkflowTemplateSummaryDto>> Handle(ListWorkflowTemplatesQuery request, CancellationToken cancellationToken)
    {
        var all = await _templates.ListAsync(includeInactive: true, cancellationToken);
        var filtered = request.IncludeInactive ? all : all.Where(t => t.IsActive).ToList();
        return filtered.Select(WorkflowTemplateDtoMapper.ToSummaryDto).ToList();
    }
}
