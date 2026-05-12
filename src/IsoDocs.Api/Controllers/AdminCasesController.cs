using IsoDocs.Application.Authorization;
using IsoDocs.Application.Cases;
using IsoDocs.Application.Cases.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/admin/cases — 管理者全公司案件視角。需 admin.full_access 權限（issue #29 [8.3]）。
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = Permissions.AdminFullAccess)]
public sealed class AdminCasesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminCasesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>取得全公司案件清單（支援狀態篩選與分頁）。</summary>
    [HttpGet("cases")]
    public async Task<ActionResult<IReadOnlyList<CaseSummaryDto>>> GetAllCases(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAllCasesQuery(status, page, pageSize), cancellationToken);
        return Ok(result);
    }
}
