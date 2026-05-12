namespace IsoDocs.Application.Cases.Queries;

/// <summary>
/// 案件查詢服務介面（只讀，供 CQRS Query 使用）。
/// </summary>
public interface ICaseQueryService
{
    Task<PagedResult<CaseSummaryDto>> ListAsync(
        ListCasesFilter filter, CancellationToken cancellationToken = default);

    Task<PagedResult<CaseSummaryDto>> SearchAsync(
        string keyword, ListCasesFilter filter, CancellationToken cancellationToken = default);
}
