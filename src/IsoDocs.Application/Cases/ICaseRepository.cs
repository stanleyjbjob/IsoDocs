using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.Cases;

/// <summary>
/// 案件聚合根的持久化介面。由 Infrastructure 的 EF Core 實作提供。
/// </summary>
public interface ICaseRepository
{
    Task<Case?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>取得案件目前進行中（InProgress 或 Pending）且 NodeOrder 最小的節點。</summary>
    Task<CaseNode?> FindActiveNodeAsync(Guid caseId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
