using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.Workflows;

/// <summary>
/// 輕量自研狀態機，管理案件節點流轉邏輯。
/// 選擇自研而非 Elsa Workflows：業務屬線性多層並行簽核，Domain Entity 已封裝
/// 個別節點狀態，引擎只需協調層間流轉，不需 Elsa 的重量級 DSL 與外部依賴。
/// </summary>
public sealed class CaseWorkflowEngine : ICaseWorkflowEngine
{
    public WorkflowTransitionResult AdvanceAfterNodeComplete(
        Case @case,
        IList<CaseNode> nodes,
        Guid completedNodeId)
    {
        var completedNode = FindNode(nodes, completedNodeId);

        // 同層並行節點必須全部完成才推進
        bool allAtOrderDone = nodes
            .Where(n => n.NodeOrder == completedNode.NodeOrder)
            .All(n => n.Status is CaseNodeStatus.Completed or CaseNodeStatus.Skipped);

        if (!allAtOrderDone)
        {
            return new WorkflowTransitionResult
            {
                Type = WorkflowTransitionType.Advanced,
                ActivatedNodeIds = []
            };
        }

        // 找下一層（NodeOrder 最小且大於當前層的 Pending 節點）
        int? nextOrder = nodes
            .Where(n => n.NodeOrder > completedNode.NodeOrder &&
                        n.Status == CaseNodeStatus.Pending)
            .Select(n => (int?)n.NodeOrder)
            .Min();

        if (nextOrder is null)
        {
            @case.Close();
            return new WorkflowTransitionResult { Type = WorkflowTransitionType.Closed };
        }

        var nextNodeIds = nodes
            .Where(n => n.NodeOrder == nextOrder && n.Status == CaseNodeStatus.Pending)
            .Select(n => n.Id)
            .ToList();

        return new WorkflowTransitionResult
        {
            Type = WorkflowTransitionType.Advanced,
            ActivatedNodeIds = nextNodeIds
        };
    }

    public WorkflowTransitionResult ReturnToPreviousNode(
        Case @case,
        IList<CaseNode> nodes,
        Guid rejectedNodeId)
    {
        var rejectedNode = FindNode(nodes, rejectedNodeId);
        rejectedNode.Return();

        // 找前一層（NodeOrder 最大且小於當前層）
        int? prevOrder = nodes
            .Where(n => n.NodeOrder < rejectedNode.NodeOrder)
            .Select(n => (int?)n.NodeOrder)
            .Max();

        if (prevOrder is null)
        {
            return new WorkflowTransitionResult
            {
                Type = WorkflowTransitionType.Returned,
                ReactivatedNodeIds = []
            };
        }

        var prevNodes = nodes.Where(n => n.NodeOrder == prevOrder).ToList();
        foreach (var node in prevNodes)
            node.Reactivate();

        return new WorkflowTransitionResult
        {
            Type = WorkflowTransitionType.Returned,
            ReactivatedNodeIds = prevNodes.Select(n => n.Id).ToList()
        };
    }

    public void VoidCase(Case @case, IList<CaseNode> nodes)
    {
        @case.Void();
        foreach (var node in nodes)
        {
            if (node.Status is not (CaseNodeStatus.Completed or CaseNodeStatus.Skipped))
                node.Skip();
        }
    }

    private static CaseNode FindNode(IList<CaseNode> nodes, Guid nodeId) =>
        nodes.FirstOrDefault(n => n.Id == nodeId)
        ?? throw new InvalidOperationException($"CaseNode {nodeId} not found.");
}
