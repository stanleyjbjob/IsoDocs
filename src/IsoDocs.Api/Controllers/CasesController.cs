using IsoDocs.Application.Authorization;
using IsoDocs.Application.Cases.Queries;
using IsoDocs.Domain.Cases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/cases — 案件清單查詢與全文搜尋（issue #27 [8.1]）。
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

    /// <summary>多條件篩選與排序的案件清單。</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.CasesRead)]
    public async Task<ActionResult<PagedResult<CaseSummaryDto>>> List(
        [FromQuery] CaseStatus? status = null,
        [FromQuery] Guid? documentTypeId = null,
        [FromQuery] DateTimeOffset? initiatedFrom = null,
        [FromQuery] DateTimeOffset? initiatedTo = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? caseNumberPrefix = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var filter = new ListCasesFilter(
            Status: status,
            DocumentTypeId: documentTypeId,
            InitiatedFrom: initiatedFrom,
            InitiatedTo: initiatedTo,
            CustomerId: customerId,
            CaseNumberPrefix: caseNumberPrefix,
            SortBy: sortBy,
            SortDescending: sortDescending,
            Page: page,
            PageSize: pageSize);

        var result = await _mediator.Send(new ListCasesQuery(filter), cancellationToken);
        return Ok(result);
    }

    /// <summary>對案件號、標題、自訂版號進行全文關鍵字搜尋。</summary>
    [HttpGet("search")]
    [Authorize(Policy = Permissions.CasesRead)]
    public async Task<ActionResult<PagedResult<CaseSummaryDto>>> Search(
        [FromQuery] string keyword,
        [FromQuery] CaseStatus? status = null,
        [FromQuery] Guid? documentTypeId = null,
        [FromQuery] DateTimeOffset? initiatedFrom = null,
        [FromQuery] DateTimeOffset? initiatedTo = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return BadRequest("keyword 為必填");

        var filter = new ListCasesFilter(
            Status: status,
            DocumentTypeId: documentTypeId,
            InitiatedFrom: initiatedFrom,
            InitiatedTo: initiatedTo,
            CustomerId: customerId,
            SortBy: sortBy,
            SortDescending: sortDescending,
            Page: page,
            PageSize: pageSize);

        var result = await _mediator.Send(new SearchCasesQuery(keyword, filter), cancellationToken);
        return Ok(result);
    }
}
