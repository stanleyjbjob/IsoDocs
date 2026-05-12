using IsoDocs.Application.Authorization;
using IsoDocs.Application.Cases.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/admin/cases — 管理者案件操作（issue #30 [2.3.2]）。
///
/// 管理者在未設定代理時可手動指派節點頂替人員。
/// </summary>
[ApiController]
[Route("api/admin/cases")]
[Authorize(Policy = Permissions.AdminCasesReassign)]
public sealed class AdminCasesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminCasesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 管理者手動指派節點頂替人員。
    /// 用於被代理人未設定代理時，由管理者直接轉派進行中的節點。
    /// </summary>
    [HttpPost("{caseId:guid}/reassign")]
    public async Task<IActionResult> Reassign(
        Guid caseId,
        [FromBody] ReassignRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new ReassignCaseNodeCommand(
            CaseId: caseId,
            NodeId: request.NodeId,
            NewAssigneeUserId: request.NewAssigneeUserId);

        await _mediator.Send(cmd, cancellationToken);
        return NoContent();
    }

    public sealed record ReassignRequest(
        Guid NodeId,
        Guid NewAssigneeUserId);
}
