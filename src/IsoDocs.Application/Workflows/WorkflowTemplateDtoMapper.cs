using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.Workflows;

public static class WorkflowTemplateDtoMapper
{
    public static WorkflowTemplateDto ToDto(WorkflowTemplate template, IReadOnlyList<WorkflowNode> nodes) =>
        new(template.Id,
            template.Code,
            template.Name,
            template.Description,
            template.Version,
            template.DefinitionJson,
            template.PublishedAt,
            template.IsActive,
            template.CreatedByUserId,
            nodes.OrderBy(n => n.NodeOrder)
                 .Select(NodeToDto)
                 .ToList());

    public static WorkflowTemplateSummaryDto ToSummaryDto(WorkflowTemplate template) =>
        new(template.Id,
            template.Code,
            template.Name,
            template.Description,
            template.Version,
            template.PublishedAt,
            template.IsActive);

    private static WorkflowNodeDto NodeToDto(WorkflowNode n) =>
        new(n.Id,
            n.NodeOrder,
            n.Name,
            (int)n.NodeType,
            n.NodeType.ToString(),
            n.RequiredRoleId,
            n.ConfigJson);
}
