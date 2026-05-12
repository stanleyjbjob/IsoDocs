using IsoDocs.Domain.Attachments;

namespace IsoDocs.Application.Attachments;

/// <summary>
/// 附件資料存取抽象。Infrastructure 層以 EF Core 實作；測試以 in-memory fake 替換。
/// </summary>
public interface IAttachmentRepository
{
    /// <summary>依案件 ID 列出未刪除附件，依上傳時間排序。</summary>
    Task<IReadOnlyList<Attachment>> ListByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default);

    /// <summary>依 ID 取得附件，找不到回傳 null。</summary>
    Task<Attachment?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Attachment attachment, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
