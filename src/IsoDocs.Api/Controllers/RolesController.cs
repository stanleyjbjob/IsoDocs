using IsoDocs.Application.Authorization;
using IsoDocs.Application.Identity.Roles;
using IsoDocs.Application.Identity.Roles.Commands;
using IsoDocs.Application.Identity.Roles.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/roles — 自訂角色管理。issue #6 [2.2.1]。
/// 讀端點需 <c>roles.read</c>，寫端點需 <c>roles.write</c>。含 AdminFullAccess 者仮同擁有這些權限。
/// </summary>
[ApiController]
[Route("api/roles")]
public sealed class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.RolesRead)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> List(
        [FromQuery] bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new ListRolesQuery(includeInactive), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.RolesRead)]
    public async Task<ActionResult<RoleDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRoleByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.RolesWrite)]
    public async Task<ActionResult<RoleDto>> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new CreateRoleCommand(
            Name: request.Name,
            Description: request.Description,
            Permissions: request.Permissions ?? Array.Empty<string>());
        var dto = await _mediator.Send(cmd, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.RolesWrite)]
    public async Task<ActionResult<RoleDto>> Update(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new UpdateRoleCommand(
            RoleId: id,
            Name: request.Name,
            Description: request.Description,
            Permissions: request.Permissions ?? Array.Empty<string>());
        var dto = await _mediator.Send(cmd, cancellationToken);
        return Ok(dto);
    }

    /// <summary>
    /// 停用角色。系統內建角色不可停用（Domain 拋 DomainException）。
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.RolesWrite)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetRoleActiveCommand(id, IsActive: false), cancellationToken);
        return NoContent();
    }

    /// <summary>重新啟用已停用角色。</summary>
    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = Permissions.RolesWrite)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SetRoleActiveCommand(id, IsActive: true), cancellationToken);
        return NoContent();
    }

    public sealed record CreateRoleRequest(
        string Name,
        string? Description,
        IReadOnlyList<string>? Permissions);

    public sealed record UpdateRoleRequest(
        string Name,
        string? Description,
        IReadOnlyList<string>? Permissions);
}
