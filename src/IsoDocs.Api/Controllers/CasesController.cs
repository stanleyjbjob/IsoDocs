using IsoDocs.Application.Cases.Export;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/cases — 案件操作。issue #28 [8.2] PDF 匯出。
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
    /// 匯出案件 PDF。包含所有欄位、節點軌跡、留言、附件清單。
    /// </summary>
    [HttpGet("{id:guid}/export/pdf")]
    [Authorize]
    public async Task<IActionResult> ExportPdf(Guid id, CancellationToken cancellationToken)
    {
        var pdfBytes = await _mediator.Send(new ExportCasePdfQuery(id), cancellationToken);
        return File(pdfBytes, "application/pdf", $"case-{id}.pdf");
    }
}
