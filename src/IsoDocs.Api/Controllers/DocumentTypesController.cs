using IsoDocs.Application.Authorization;
using IsoDocs.Application.DocumentTypes;
using IsoDocs.Application.DocumentTypes.Commands;
using IsoDocs.Application.DocumentTypes.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/document-types — 文件類型管理與自動編碼取號。issue #16 [5.1.1]。
/// </summary>
[ApiController]
[Route("api/document-types")]
public sealed class DocumentTypesController : ControllerBase
{
    private readonly IMediator _mediator;

    public DocumentTypesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.DocumentTypesRead)]
    public async Task<ActionResult<IReadOnlyList<DocumentTypeDto>>> List(
        [FromQuery] bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ListDocumentTypesQuery(includeInactive), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.DocumentTypesRead)]
    public async Task<ActionResult<DocumentTypeDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDocumentTypeByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.DocumentTypesWrite)]
    public async Task<ActionResult<DocumentTypeDto>> Create(
        [FromBody] CreateDocumentTypeRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new CreateDocumentTypeCommand(request.CompanyCode, request.Code, request.Name);
        var dto = await _mediator.Send(cmd, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.DocumentTypesWrite)]
    public async Task<ActionResult<DocumentTypeDto>> Update(
        Guid id,
        [FromBody] UpdateDocumentTypeRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new UpdateDocumentTypeCommand(id, request.Name, request.IsActive);
        var dto = await _mediator.Send(cmd, cancellationToken);
        return Ok(dto);
    }

    /// <summary>
    /// 向指定文件類型取得下一個自動編碼（含年度重置與樂觀鎖重試）。
    /// </summary>
    [HttpPost("{id:guid}/acquire-next-code")]
    [Authorize(Policy = Permissions.DocumentTypesWrite)]
    public async Task<ActionResult<AcquireNextCodeResponse>> AcquireNextCode(
        Guid id, CancellationToken cancellationToken)
    {
        var code = await _mediator.Send(new AcquireNextCodeCommand(id), cancellationToken);
        return Ok(new AcquireNextCodeResponse(code));
    }

    public sealed record CreateDocumentTypeRequest(string CompanyCode, string Code, string Name);
    public sealed record UpdateDocumentTypeRequest(string Name, bool IsActive);
    public sealed record AcquireNextCodeResponse(string Code);
}
