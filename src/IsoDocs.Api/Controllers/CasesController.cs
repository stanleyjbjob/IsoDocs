using IsoDocs.Application.Authorization;
using IsoDocs.Application.Cases;
using IsoDocs.Application.Cases.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/cases — 案件管理。issue #20 [5.4.1]。
/// 寫入端點需 <c>cases.write</c> 權限。
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
    /// 修改預計完成時間。首次設定寫入 Case.OriginalExpectedAt；
    /// 後續修改另更新當前節點的 CaseNode.ModifiedExpectedAt。
    /// </summary>
    [HttpPut("{id:guid}/expected-completion")]
    [Authorize(Policy = Permissions.CasesWrite)]
    public async Task<ActionResult<CaseDto>> UpdateExpectedCompletion(
        Guid id,
        [FromBody] UpdateExpectedCompletionRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new UpdateExpectedCompletionCommand(id, request.ExpectedAt);
        var dto = await _mediator.Send(cmd, cancellationToken);
        return Ok(dto);
    }

    public sealed record UpdateExpectedCompletionRequest(DateTimeOffset ExpectedAt);
}
