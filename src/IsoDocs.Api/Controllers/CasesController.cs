using System.Security.Claims;
using IsoDocs.Application.Authorization;
using IsoDocs.Application.Cases;
using IsoDocs.Application.Cases.Commands;
using IsoDocs.Application.Cases.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/cases — 案件管理。issue #17 [5.3.1] 子流程衍生與主子流程雙向關聯。
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

    /// <summary>衍生子流程：以指定案件為父案建立子案件，並建立 Subprocess 雙向關聯。</summary>
    [HttpPost("{id:guid}/actions/spawn-child")]
    [Authorize(Policy = Permissions.CasesWrite)]
    public async Task<ActionResult<SpawnChildCaseResult>> SpawnChild(
        Guid id,
        [FromBody] SpawnChildRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var cmd = new SpawnChildCaseCommand(
            ParentCaseId: id,
            Title: request.Title,
            InitiatedByUserId: userId);
        var result = await _mediator.Send(cmd, cancellationToken);
        return CreatedAtAction(nameof(GetRelations), new { id = result.ChildCaseId }, result);
    }

    /// <summary>取得案件的雙向關聯清單（不論該案件為父或子）。</summary>
    [HttpGet("{id:guid}/relations")]
    [Authorize(Policy = Permissions.CasesRead)]
    public async Task<ActionResult<IReadOnlyList<CaseRelationDto>>> GetRelations(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCaseRelationsQuery(id), cancellationToken);
        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue("oid")
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("無法從 Token 取得使用者識別碼");
    }

    public sealed record SpawnChildRequest(string Title);
}
