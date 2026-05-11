namespace IsoDocs.Domain.Cases;

/// <summary>
/// 案件關聯類型。
/// </summary>
public enum CaseRelationType
{
    /// <summary>主子流程關聯（issue [5.3.1]）。</summary>
    Subprocess = 1,
    /// <summary>結案後重開新案（issue [5.3.4]）。</summary>
    Reopen = 2,
    /// <summary>參考關聯（其他業務關聯）。</summary>
    Reference = 3
}
