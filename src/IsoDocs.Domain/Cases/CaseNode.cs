using IsoDocs.Domain.Common;

namespace IsoDocs.Domain.Cases;

/// <summary>
/// 案件節點實例（依流程範本複製出來的實際處理節點）。
/// 保留各節點狀態、承辦人、預計與實際完成時間。
/// </summary>
public class CaseNode : Entity<Guid>
{
    public Guid CaseId { get; protected set; }
    public Guid WorkflowNodeId { get; protected set; }
    public int NodeOrder { get; protected set; }
    public string NodeName { get; protected set; } = string.Empty;
    public Guid? AssigneeUserId { get; protected set; }
    public CaseNodeStatus Status { get; protected set; } = CaseNodeStatus.Pending;

    /// <summary>本節點修改後的預計完成時間（保留本節點設定值，issue [5.4.1]）。</summary>
    public DateTimeOffset? ModifiedExpectedAt { get; protected set; }
    /// <summary>本節點進入時的原始預計完成時間。</summary>
    public DateTimeOffset? OriginalExpectedAt { get; protected set; }
    public DateTimeOffset? StartedAt { get; protected set; }
    public DateTimeOffset? CompletedAt { get; protected set; }

    private CaseNode() { }

    public CaseNode(Guid id, Guid caseId, Guid workflowNodeId, int nodeOrder, string nodeName, Guid? assigneeUserId, DateTimeOffset? originalExpectedAt)
    {
        Id = id;
        CaseId = caseId;
        WorkflowNodeId = workflowNodeId;
        NodeOrder = nodeOrder;
        NodeName = nodeName;
        AssigneeUserId = assigneeUserId;
        OriginalExpectedAt = originalExpectedAt;
    }

    public void Accept(Guid acceptedByUserId)
    {
        if (Status != CaseNodeStatus.Pending)
            throw new DomainException("case_node.invalid_status", "只有待處理狀態可接單");
        AssigneeUserId = acceptedByUserId;
        Status = CaseNodeStatus.InProgress;
        StartedAt = DateTimeOffset.UtcNow;
        UpdatedAt = StartedAt;
    }

    public void Complete()
    {
        if (Status != CaseNodeStatus.InProgress && Status != CaseNodeStatus.Pending)
            throw new DomainException("case_node.invalid_status", "不可完成未進行中的節點");
        Status = CaseNodeStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CompletedAt;
    }

    public void Return()
    {
        Status = CaseNodeStatus.Returned;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Skip()
    {
        Status = CaseNodeStatus.Skipped;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ModifyExpected(DateTimeOffset modifiedExpectedAt)
    {
        ModifiedExpectedAt = modifiedExpectedAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reassign(Guid newAssigneeUserId)
    {
        AssigneeUserId = newAssigneeUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
