using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.Cases;

/// <summary>
/// 案件資料存取抽象。Infrastructure 層以 EF Core 實作；測試以 in-memory fake 實作。
/// </summary>
public interface ICaseRepository
{
    Task<Case?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>找出案件中最優先的待處理或進行中節點（依 NodeOrder 升冪）。</summary>
    Task<CaseNode?> FindActiveNodeAsync(Guid caseId, CancellationToken cancellationToken = default);

    Task AddAsync(Case @case, CancellationToken cancellationToken = default);
    Task AddNodeAsync(CaseNode node, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
