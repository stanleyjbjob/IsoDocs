namespace IsoDocs.Application.Cases.Comments;

public sealed record CommentDto(
    Guid Id,
    Guid CaseId,
    Guid AuthorUserId,
    string Body,
    Guid? ParentCommentId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
