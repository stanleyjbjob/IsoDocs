using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.Workflows.Commands;

/// <summary>
/// 建立或更新流程範本時傳入的節點定義。
/// </summary>
public sealed record NodeInput(
    int NodeOrder,
    string Name,
    WorkflowNodeType NodeType,
    Guid? RequiredRoleId,
    string? ConfigJson = null);
