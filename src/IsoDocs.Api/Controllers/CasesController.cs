using IsoDocs.Application.Auth;
using IsoDocs.Application.Authorization;
using IsoDocs.Application.Cases.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/cases — 案件管理。issue [5.x]。
/// </summary>
[ApiController]
[Route("api/cases")]
public sealed class CasesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserSyncService _userSync;

    public CasesController(IMediator mediator, IUserSyncService userSync)
    {
        _mediator = mediator;
        _userSync = userSync;
    }

    /// <summary>
    /// 作廢案件。主單作廢時自動連鎖作廢所有未完成子流程。
    /// 需 cases.void 權限（顧問或管理者）。issue [5.3.3]。
    /// </summary>
    [HttpPost("{id:guid}/actions/void")]
    [Authorize(Policy = Permissions.CasesVoid)]
    public async Task<IActionResult> Void(
        Guid id,
        [FromBody] VoidCaseRequest request,
        CancellationToken cancellationToken)
    {
        var principal = User.ToAzureAdUserPrincipal();
        var actor = await _userSync.UpsertFromAzureAdAsync(principal, cancellationToken);

        var cmd = new VoidCaseCommand(
            CaseId: id,
            ActorUserId: actor.Id,
            Comment: request.Comment);
        await _mediator.Send(cmd, cancellationToken);
        return NoContent();
    }

    public sealed record VoidCaseRequest(string? Comment);
}
