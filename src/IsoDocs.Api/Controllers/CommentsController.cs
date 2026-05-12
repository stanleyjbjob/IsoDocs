using IsoDocs.Application.Auth;
using IsoDocs.Application.Authorization;
using IsoDocs.Application.Cases.Comments;
using IsoDocs.Application.Cases.Comments.Commands;
using IsoDocs.Application.Cases.Comments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/cases/{caseId}/comments — 案件留言。issue #25 [7.1]。
/// 讀需 cases.comments.read，寫需 cases.comments.write。
/// </summary>
[ApiController]
[Route("api/cases/{caseId:guid}/comments")]
public sealed class CommentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserSyncService _userSync;

    public CommentsController(IMediator mediator, IUserSyncService userSync)
    {
        _mediator = mediator;
        _userSync = userSync;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.CasesCommentsRead)]
    public async Task<ActionResult<IReadOnlyList<CommentDto>>> List(
        Guid caseId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListCommentsQuery(caseId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.CasesCommentsWrite)]
    public async Task<ActionResult<CommentDto>> Add(
        Guid caseId,
        [FromBody] AddCommentRequest request,
        CancellationToken cancellationToken)
    {
        var principal = User.ToAzureAdUserPrincipal();
        var user = await _userSync.UpsertFromAzureAdAsync(principal, cancellationToken);
        var cmd = new AddCommentCommand(caseId, user.Id, request.Body, request.ParentCommentId);
        var dto = await _mediator.Send(cmd, cancellationToken);
        return CreatedAtAction(nameof(List), new { caseId }, dto);
    }

    public sealed record AddCommentRequest(string Body, Guid? ParentCommentId);
}
