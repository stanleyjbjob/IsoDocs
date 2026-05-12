using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.Cases;

/// <summary>
/// 案件資料存取抽象。Infrastructure 層以 EF Core 實作；測試以 in-memory fake 實作。
/// </summary>
public interface ICaseRepository
{
    Task<Case?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>取得指定主單下所有「子流程」類型的關聯案件（含子案件本身）。</summary>
    Task<IReadOnlyList<Case>> FindSubprocessChildrenAsync(Guid parentCaseId, CancellationToken cancellationToken = default);

    Task AddCaseActionAsync(CaseAction action, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
