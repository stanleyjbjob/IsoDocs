using IsoDocs.Application.Authorization;
using IsoDocs.Application.Cases;
using IsoDocs.Application.Cases.Commands;
using IsoDocs.Application.Cases.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/cases — 案件管理。issue #32 [5.3.4] 重開新案與關聯查詢。
/// </summary>
[ApiController]
[Route("api/cases")]
public sealed class CasesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CasesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>查詢指定案件的所有關聯（主子流程、重開、參考）。</summary>
    [HttpGet("{id:guid}/relations")]
    [Authorize(Policy = Permissions.CasesRead)]
    public async Task<ActionResult<IReadOnlyList<CaseRelationDto>>> GetRelations(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCaseRelationsQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>對已結案的案件重開新案，並建立 CaseRelation（RelationType=Reopen）。</summary>
    [HttpPost("{id:guid}/actions/reopen")]
    [Authorize(Policy = Permissions.CasesWrite)]
    public async Task<ActionResult<ReopenCaseResult>> Reopen(
        Guid id,
        [FromBody] ReopenCaseRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var cmd = new ReopenCaseCommand(
            OriginalCaseId: id,
            NewTitle: request.NewTitle,
            RequestedByUserId: userId);
        var result = await _mediator.Send(cmd, cancellationToken);
        return CreatedAtAction(nameof(GetRelations), new { id = result.NewCaseId }, result);
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value
                  ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var userId) ? userId : Guid.Empty;
    }

    public sealed record ReopenCaseRequest(string NewTitle);
}
