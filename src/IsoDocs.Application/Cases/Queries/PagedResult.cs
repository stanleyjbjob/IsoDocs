namespace IsoDocs.Application.Cases.Queries;

/// <summary>
/// 分頁查詢結果包裝器。
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
