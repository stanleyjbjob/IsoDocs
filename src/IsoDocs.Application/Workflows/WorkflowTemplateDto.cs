namespace IsoDocs.Application.Workflows;

public sealed record WorkflowTemplateSummaryDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    int Version,
    DateTimeOffset? PublishedAt,
    bool IsActive);

public sealed record WorkflowTemplateDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    int Version,
    string DefinitionJson,
    DateTimeOffset? PublishedAt,
    bool IsActive,
    Guid CreatedByUserId,
    IReadOnlyList<WorkflowNodeDto> Nodes);

public sealed record WorkflowNodeDto(
    Guid Id,
    int NodeOrder,
    string Name,
    int NodeType,
    string NodeTypeName,
    Guid? RequiredRoleId,
    string ConfigJson);
