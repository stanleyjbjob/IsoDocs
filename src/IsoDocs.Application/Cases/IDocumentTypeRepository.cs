using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.Cases;

/// <summary>
/// 文件類型資料存取抽象（含流水號取號）。
/// 取號後需透過 SaveChangesAsync 持久化 CurrentSequence 更新。
/// </summary>
public interface IDocumentTypeRepository
{
    /// <summary>依 Id 取得文件類型（追蹤版本，以便 AcquireNext 後自動存回）。</summary>
    Task<DocumentType?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
