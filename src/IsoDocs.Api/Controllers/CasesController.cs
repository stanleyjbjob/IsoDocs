using IsoDocs.Application.Auth;
using IsoDocs.Application.Authorization;
using IsoDocs.Application.Cases;
using IsoDocs.Application.Cases.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/cases — 工作需求單主流程端點。issue #9 [5.2.1]。
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

    /// <summary>發起新工作需求單（自動取號）。</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.CasesWrite)]
    public async Task<ActionResult<CaseDto>> Create(
        [FromBody] CreateCaseRequest request,
        CancellationToken cancellationToken)
    {
        var principal = User.ToAzureAdUserPrincipal();
        var user = await _userSync.UpsertFromAzureAdAsync(principal, cancellationToken);

        var cmd = new CreateCaseCommand(
            Title: request.Title,
            DocumentTypeId: request.DocumentTypeId,
            WorkflowTemplateId: request.WorkflowTemplateId,
            ExpectedCompletionAt: request.ExpectedCompletionAt,
            CustomerId: request.CustomerId,
            InitiatedByUserId: user.Id);

        var dto = await _mediator.Send(cmd, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>取得案件詳情（完整查詢由 issue #27 [8.1] 實作）。</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.CasesRead)]
    public Task<ActionResult<CaseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        // TODO: issue #27 [8.1] — 完整案件查詢端點
        return Task.FromResult<ActionResult<CaseDto>>(NotFound());
    }

    /// <summary>指派案件承辦人至當前待處理節點。</summary>
    [HttpPut("{id:guid}/assign")]
    [Authorize(Policy = Permissions.CasesWrite)]
    public async Task<IActionResult> Assign(
        Guid id,
        [FromBody] AssignCaseRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new AssignCaseCommand(CaseId: id, AssigneeUserId: request.AssigneeUserId);
        await _mediator.Send(cmd, cancellationToken);
        return NoContent();
    }

    public sealed record CreateCaseRequest(
        string Title,
        Guid DocumentTypeId,
        Guid WorkflowTemplateId,
        DateTimeOffset? ExpectedCompletionAt,
        Guid? CustomerId);

    public sealed record AssignCaseRequest(Guid AssigneeUserId);
}
