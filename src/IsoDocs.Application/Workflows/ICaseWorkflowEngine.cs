using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.Workflows;

/// <summary>
/// 工作流引擎，管理案件狀態機節點流轉。
/// </summary>
public interface ICaseWorkflowEngine
{
    /// <summary>
    /// 節點完成後推進流程：啟動下一層節點，或在所有節點皆完成時結案。
    /// 若同層仍有並行節點未完成，則等待。
    /// </summary>
    WorkflowTransitionResult AdvanceAfterNodeComplete(
        Case @case,
        IList<CaseNode> nodes,
        Guid completedNodeId);

    /// <summary>
    /// 退回至前一處理節點：將拒絕節點標為 Returned，並重新啟動前一層節點。
    /// </summary>
    WorkflowTransitionResult ReturnToPreviousNode(
        Case @case,
        IList<CaseNode> nodes,
        Guid rejectedNodeId);

    /// <summary>
    /// 作廢案件：跳過所有尚未完成的節點，並標記案件為已作廢。
    /// </summary>
    void VoidCase(Case @case, IList<CaseNode> nodes);
}
