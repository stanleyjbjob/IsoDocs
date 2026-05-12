namespace IsoDocs.Application.Cases;

/// <summary>
/// 首頁儀表板所需的案件讀取服務（issue #29 [8.3]）。
/// 由 Infrastructure 層實作，透過 EF Core 查詢資料庫。
/// </summary>
public interface ICaseDashboardService
{
    Task<IReadOnlyList<TodoItemDto>> GetMyTodosAsync(
        Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CaseSummaryDto>> GetMyInitiatedCasesAsync(
        Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CaseSummaryDto>> GetAllCasesAsync(
        string? statusFilter, int page, int pageSize, CancellationToken cancellationToken);
}
