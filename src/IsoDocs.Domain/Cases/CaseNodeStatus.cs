namespace IsoDocs.Domain.Cases;

/// <summary>
/// 案件節點實例狀態。
/// </summary>
public enum CaseNodeStatus
{
    /// <summary>待處理（潛伏，進到這個節點但未接單）。</summary>
    Pending = 1,
    /// <summary>已接單進行中。</summary>
    InProgress = 2,
    /// <summary>已完成。</summary>
    Completed = 3,
    /// <summary>已退回到前一處理節點。</summary>
    Returned = 4,
    /// <summary>跳過（例如連鎖作廢後未處理節點跳過）。</summary>
    Skipped = 5
}
