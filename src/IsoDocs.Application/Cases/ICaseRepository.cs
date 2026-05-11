using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.Cases;

/// <summary>
/// 案件 Repository 介面。提供案件與節點的讀取及動作軌跡的寫入。
/// </summary>
public interface ICaseRepository
{
    Task<Case?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CaseNode>> GetNodesAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task AddActionAsync(CaseAction action, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
