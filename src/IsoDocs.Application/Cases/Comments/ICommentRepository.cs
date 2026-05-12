using IsoDocs.Domain.Communications;

namespace IsoDocs.Application.Cases.Comments;

/// <summary>
/// 案件留言資料存取抽象。Infrastructure 層以 EF Core 實作；測試以 in-memory fake 實作。
/// </summary>
public interface ICommentRepository
{
    /// <summary>確認案件是否存在（留言前需驗證）。</summary>
    Task<bool> CaseExistsAsync(Guid caseId, CancellationToken cancellationToken = default);

    /// <summary>列出指定案件的所有未刪除留言，依 CreatedAt 昇冪排序。</summary>
    Task<IReadOnlyList<Comment>> ListByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default);

    Task AddAsync(Comment comment, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
