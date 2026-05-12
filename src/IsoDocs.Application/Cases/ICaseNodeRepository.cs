using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.Cases;

/// <summary>
/// 案件節點資料存取抽象。
/// 本次 issue #30 僅暴露管理者手動指派所需的最小方法集，後續 issue 會擴充。
/// </summary>
public interface ICaseNodeRepository
{
    /// <summary>取得指定案件的指定節點。</summary>
    Task<CaseNode?> GetByCaseAndNodeAsync(Guid caseId, Guid nodeId, CancellationToken cancellationToken = default);

    /// <summary>取得指定案件目前進行中（InProgress 或 Pending）的節點清單。</summary>
    Task<IReadOnlyList<CaseNode>> ListActiveByCaseAsync(Guid caseId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
