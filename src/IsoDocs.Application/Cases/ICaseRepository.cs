using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.Cases;

/// <summary>
/// 案件資料存取抽象。Infrastructure 層以 EF Core 實作；測試以 in-memory fake 實作。
/// </summary>
public interface ICaseRepository
{
    Task<Case?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CaseNode?> FindNodeByIdAsync(Guid nodeId, CancellationToken cancellationToken = default);

    /// <summary>取得案件全部簽核動作，依 ActionAt 升冪排序，包含節點名稱。</summary>
    Task<IReadOnlyList<SignOffEntryDto>> GetSignOffTrailAsync(Guid caseId, CancellationToken cancellationToken = default);

    Task AddActionAsync(CaseAction action, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
