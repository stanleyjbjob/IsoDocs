using IsoDocs.Application.Authorization;
using IsoDocs.Application.Workflows;
using IsoDocs.Application.Workflows.Commands;
using IsoDocs.Application.Workflows.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/workflow-templates — 流程範本版本化管理。issue #12 [3.2.1]。
/// 讀端點需 <c>templates.read</c>，寫端點需 <c>templates.write</c>。
/// </summary>
[ApiController]
[Route("api/workflow-templates")]
public sealed class WorkflowTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public WorkflowTemplatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.TemplatesRead)]
    public async Task<ActionResult<IReadOnlyList<WorkflowTemplateSummaryDto>>> List(
        [FromQuery] bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ListWorkflowTemplatesQuery(includeInactive), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.TemplatesRead)]
    public async Task<ActionResult<WorkflowTemplateDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWorkflowTemplateByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.TemplatesWrite)]
    public async Task<ActionResult<WorkflowTemplateDto>> Create(
        [FromBody] CreateWorkflowTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var cmd = new CreateWorkflowTemplateCommand(
            request.Code, request.Name, request.Description,
            userId, request.Nodes ?? Array.Empty<NodeInput>());
        var dto = await _mediator.Send(cmd, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.TemplatesWrite)]
    public async Task<ActionResult<WorkflowTemplateDto>> Update(
        Guid id,
        [FromBody] UpdateWorkflowTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new UpdateWorkflowTemplateCommand(
            id, request.Name, request.Description,
            request.Nodes ?? Array.Empty<NodeInput>());
        var dto = await _mediator.Send(cmd, cancellationToken);
        return Ok(dto);
    }

    /// <summary>
    /// 發行目前草稿版本，使新案件套用此版本。已發行版本再次呼叫會回 422。
    /// </summary>
    [HttpPut("{id:guid}/publish")]
    [Authorize(Policy = Permissions.TemplatesWrite)]
    public async Task<ActionResult<WorkflowTemplateDto>> Publish(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(new PublishWorkflowTemplateCommand(id), cancellationToken);
        return Ok(dto);
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    public sealed record CreateWorkflowTemplateRequest(
        string Code,
        string Name,
        string? Description,
        IReadOnlyList<NodeInput>? Nodes);

    public sealed record UpdateWorkflowTemplateRequest(
        string Name,
        string? Description,
        IReadOnlyList<NodeInput>? Nodes);
}
