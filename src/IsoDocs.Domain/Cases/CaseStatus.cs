namespace IsoDocs.Domain.Cases;

/// <summary>
/// 案件狀態。
/// </summary>
public enum CaseStatus
{
    /// <summary>進行中（含各個節點未結案狀態）。</summary>
    InProgress = 1,
    /// <summary>已結案。</summary>
    Closed = 2,
    /// <summary>已作廢。</summary>
    Voided = 3
}
