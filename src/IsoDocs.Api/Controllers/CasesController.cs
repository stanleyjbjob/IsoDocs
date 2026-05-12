using IsoDocs.Application.Auth;
using IsoDocs.Application.Authorization;
using IsoDocs.Application.Cases;
using IsoDocs.Application.Cases.Commands;
using IsoDocs.Application.Cases.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/cases — 案件管理。issue #31 [5.4.3] 文件發行核准簽核軌跡。
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

    /// <summary>
    /// 取得案件完整簽核軌跡。
    /// GET /api/cases/{caseId}/sign-off-trail
    /// </summary>
    [HttpGet("{caseId:guid}/sign-off-trail")]
    [Authorize(Policy = Permissions.CasesRead)]
    public async Task<ActionResult<IReadOnlyList<SignOffEntryDto>>> GetSignOffTrail(
        Guid caseId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSignOffTrailQuery(caseId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 對案件核准節點提交文件發行簽核。
    /// POST /api/cases/{caseId}/actions/sign-off
    /// </summary>
    [HttpPost("{caseId:guid}/actions/sign-off")]
    [Authorize(Policy = Permissions.CasesSignOff)]
    public async Task<ActionResult<SignOffEntryDto>> SignOff(
        Guid caseId,
        [FromBody] SignOffRequest request,
        CancellationToken cancellationToken)
    {
        var principal = User.ToAzureAdUserPrincipal();

        if (!principal.IsAuthenticated)
            return Unauthorized(new { code = "auth.missing_oid", message = "無法識別使用者身份" });

        var user = await _userSync.UpsertFromAzureAdAsync(principal, cancellationToken);

        var cmd = new SignOffCaseNodeCommand(
            CaseId: caseId,
            CaseNodeId: request.CaseNodeId,
            ActorUserId: user.Id,
            Comment: request.Comment);

        var dto = await _mediator.Send(cmd, cancellationToken);
        return Ok(dto);
    }

    public sealed record SignOffRequest(Guid CaseNodeId, string? Comment);
}
