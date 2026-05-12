using IsoDocs.Application.Common.Messaging;

namespace IsoDocs.Application.Cases.Comments.Queries;

/// <summary>
/// 列出指定案件的留言。對應 GET /api/cases/{caseId}/comments。
/// </summary>
public sealed record ListCommentsQuery(Guid CaseId) : IQuery<IReadOnlyList<CommentDto>>;

public sealed class ListCommentsQueryHandler : IQueryHandler<ListCommentsQuery, IReadOnlyList<CommentDto>>
{
    private readonly ICommentRepository _comments;

    public ListCommentsQueryHandler(ICommentRepository comments)
    {
        _comments = comments;
    }

    public async Task<IReadOnlyList<CommentDto>> Handle(
        ListCommentsQuery request, CancellationToken cancellationToken)
    {
        var comments = await _comments.ListByCaseIdAsync(request.CaseId, cancellationToken);
        return comments
            .Select(c => new CommentDto(
                c.Id, c.CaseId, c.AuthorUserId, c.Body,
                c.ParentCommentId, c.CreatedAt, c.UpdatedAt))
            .ToList();
    }
}
