using IsoDocs.Application.Authorization;
using IsoDocs.Application.Cases.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/cases — 案件管理。issue #33 [5.4.2] 文件版號人為自訂。
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

    /// <summary>
    /// 設定案件的自訂版號。版號格式：英數字、點、連字號、底線，首字必為英數，最多 20 字元。
    /// </summary>
    [HttpPut("{id:guid}/version-number")]
    [Authorize(Policy = Permissions.CasesWrite)]
    public async Task<ActionResult<VersionNumberResponse>> SetVersionNumber(
        Guid id,
        [FromBody] SetVersionNumberRequest request,
        CancellationToken cancellationToken)
    {
        var version = await _mediator.Send(
            new SetCustomVersionNumberCommand(id, request.VersionNumber), cancellationToken);
        return Ok(new VersionNumberResponse(version));
    }

    public sealed record SetVersionNumberRequest(string VersionNumber);
    public sealed record VersionNumberResponse(string VersionNumber);
}
