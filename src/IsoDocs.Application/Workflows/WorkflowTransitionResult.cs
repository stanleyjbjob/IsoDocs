namespace IsoDocs.Application.Workflows;

public enum WorkflowTransitionType
{
    /// <summary>流程推進至下一層節點（含並行節點等待中）。</summary>
    Advanced,
    /// <summary>所有節點已完成，案件自動結案。</summary>
    Closed,
    /// <summary>退回至前一處理節點。</summary>
    Returned,
    /// <summary>案件已作廢，剩餘節點已跳過。</summary>
    Voided
}

public sealed class WorkflowTransitionResult
{
    public WorkflowTransitionType Type { get; init; }

    /// <summary>本次被啟動（可接單）的節點 ID 清單；並行節點可能有多個。</summary>
    public IReadOnlyList<Guid> ActivatedNodeIds { get; init; } = [];

    /// <summary>退回後重新啟動的前一層節點 ID 清單。</summary>
    public IReadOnlyList<Guid> ReactivatedNodeIds { get; init; } = [];
}
