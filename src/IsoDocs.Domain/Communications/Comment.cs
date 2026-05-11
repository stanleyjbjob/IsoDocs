using IsoDocs.Domain.Common;

namespace IsoDocs.Domain.Communications;

/// <summary>
/// 案件留言（issue [7.1]）。可支持反次互動。
/// </summary>
public class Comment : Entity<Guid>, IAggregateRoot
{
    public Guid CaseId { get; protected set; }
    public Guid AuthorUserId { get; protected set; }
    public string Body { get; protected set; } = string.Empty;
    public Guid? ParentCommentId { get; protected set; }
    public bool IsDeleted { get; protected set; }

    private Comment() { }

    public Comment(Guid id, Guid caseId, Guid authorUserId, string body, Guid? parentCommentId)
    {
        Id = id;
        CaseId = caseId;
        AuthorUserId = authorUserId;
        Body = body;
        ParentCommentId = parentCommentId;
    }

    public void Edit(string body)
    {
        Body = body;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>軟刪除。對應需求中「不因結案或作廢刪除」的設計原則。</summary>
    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
