using MediatR;

namespace IsoDocs.Application.Cases.Comments.Events;

/// <summary>
/// 新留言建立後發布的 MediatR 通知。
/// 通知 handler 由 issue #24 [6.2] 實作；此處僅定義事件合約。
/// </summary>
public sealed record NewCommentCreatedNotification(
    Guid CommentId,
    Guid CaseId,
    Guid AuthorUserId) : INotification;
