using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.Cases;

/// <summary>
/// 流程範本與節點定義資料存取抽象（唯讀）。
/// </summary>
public interface IWorkflowRepository
{
    Task<WorkflowTemplate?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>取得指定範本版本的所有節點定義，依 NodeOrder 升冪排序。</summary>
    Task<IReadOnlyList<WorkflowNode>> ListNodesByTemplateAsync(
        Guid templateId, int templateVersion, CancellationToken cancellationToken = default);
}
