using IsoDocs.Application.Auth;
using IsoDocs.Application.Authorization;
using IsoDocs.Application.Cases.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/cases — 案件動作端點。issue #15 [5.2.2]。
/// 接單、回覆結案、核准/結案、退回至前一節點。
/// </summary>
[ApiController]
[Route("api/cases")]
[Authorize]
public sealed class CasesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserSyncService _userSync;

    public CasesController(IMediator mediator, IUserSyncService userSync)
    {
        _mediator = mediator;
        _userSync = userSync;
    }

    /// <summary>接單：將目前最靠前的 Pending 節點轉為 InProgress。</summary>
    [HttpPost("{id:guid}/actions/accept")]
    [Authorize(Policy = Permissions.CasesWrite)]
    public async Task<IActionResult> Accept(
        Guid id,
        [FromBody] CaseActionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(cancellationToken);
        await _mediator.Send(new AcceptCaseCommand(id, userId, request.Comment), cancellationToken);
        return NoContent();
    }

    /// <summary>回覆結案：完成目前 InProgress 節點；無下一節點時自動結案。</summary>
    [HttpPost("{id:guid}/actions/reply-close")]
    [Authorize(Policy = Permissions.CasesWrite)]
    public async Task<IActionResult> ReplyClose(
        Guid id,
        [FromBody] CaseActionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(cancellationToken);
        await _mediator.Send(new ReplyCaseCommand(id, userId, request.Comment), cancellationToken);
        return NoContent();
    }

    /// <summary>核准/結案：核准目前 InProgress 節點；無下一節點時自動結案。</summary>
    [HttpPost("{id:guid}/actions/approve")]
    [Authorize(Policy = Permissions.CasesWrite)]
    public async Task<IActionResult> Approve(
        Guid id,
        [FromBody] CaseActionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(cancellationToken);
        await _mediator.Send(new ApproveCaseCommand(id, userId, request.Comment), cancellationToken);
        return NoContent();
    }

    /// <summary>退回：將目前節點標為 Returned，重新啟用前一個 Completed 節點。</summary>
    [HttpPost("{id:guid}/actions/reject")]
    [Authorize(Policy = Permissions.CasesWrite)]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] CaseActionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = await ResolveUserIdAsync(cancellationToken);
        await _mediator.Send(new RejectCaseCommand(id, userId, request.Comment), cancellationToken);
        return NoContent();
    }

    private async Task<Guid> ResolveUserIdAsync(CancellationToken cancellationToken)
    {
        var principal = User.ToAzureAdUserPrincipal();
        var user = await _userSync.UpsertFromAzureAdAsync(principal, cancellationToken);
        return user.Id;
    }

    public sealed record CaseActionRequest(string? Comment);
}
