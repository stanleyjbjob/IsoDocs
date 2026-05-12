using FluentValidation;
using IsoDocs.Application.Cases.Comments.Events;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;
using IsoDocs.Domain.Communications;
using MediatR;

namespace IsoDocs.Application.Cases.Comments.Commands;

/// <summary>
/// 在案件新增留言。對應 POST /api/cases/{caseId}/comments。
/// </summary>
public sealed record AddCommentCommand(
    Guid CaseId,
    Guid AuthorUserId,
    string Body,
    Guid? ParentCommentId) : ICommand<CommentDto>;

public sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.AuthorUserId).NotEmpty();
        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("留言內容必填")
            .MaximumLength(4000).WithMessage("留言不可超過 4000 字");
    }
}

public sealed class AddCommentCommandHandler : ICommandHandler<AddCommentCommand, CommentDto>
{
    private readonly ICommentRepository _comments;
    private readonly IPublisher _publisher;

    public AddCommentCommandHandler(ICommentRepository comments, IPublisher publisher)
    {
        _comments = comments;
        _publisher = publisher;
    }

    public async Task<CommentDto> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var exists = await _comments.CaseExistsAsync(request.CaseId, cancellationToken);
        if (!exists)
            throw new DomainException("COMMENT/CASE_NOT_FOUND", $"案件 {request.CaseId} 不存在。");

        var comment = new Comment(
            Guid.NewGuid(),
            request.CaseId,
            request.AuthorUserId,
            request.Body,
            request.ParentCommentId);

        await _comments.AddAsync(comment, cancellationToken);
        await _comments.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(
            new NewCommentCreatedNotification(comment.Id, comment.CaseId, comment.AuthorUserId),
            cancellationToken);

        return new CommentDto(
            comment.Id, comment.CaseId, comment.AuthorUserId,
            comment.Body, comment.ParentCommentId,
            comment.CreatedAt, comment.UpdatedAt);
    }
}
