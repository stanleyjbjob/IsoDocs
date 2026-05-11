using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;

namespace IsoDocs.Application.Workflows.Queries;

/// <summary>
/// 依 Id 取得流程範本（含節點定義）。對應 GET /api/workflow-templates/{id}。
/// </summary>
public sealed record GetWorkflowTemplateByIdQuery(Guid TemplateId) : IQuery<WorkflowTemplateDto>;

public sealed class GetWorkflowTemplateByIdQueryHandler : IQueryHandler<GetWorkflowTemplateByIdQuery, WorkflowTemplateDto>
{
    private readonly IWorkflowTemplateRepository _templates;

    public GetWorkflowTemplateByIdQueryHandler(IWorkflowTemplateRepository templates)
    {
        _templates = templates;
    }

    public async Task<WorkflowTemplateDto> Handle(GetWorkflowTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        var template = await _templates.FindByIdAsync(request.TemplateId, cancellationToken)
            ?? throw new DomainException(WorkflowErrorCodes.TemplateNotFound, $"找不到流程範本 {request.TemplateId}。");

        var nodes = await _templates.ListNodesAsync(template.Id, template.Version, cancellationToken);
        return WorkflowTemplateDtoMapper.ToDto(template, nodes);
    }
}
