using IsoDocs.Application.Authorization;
using IsoDocs.Application.Identity.UserRoles.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/users/{userId}/roles — 使用者角色指派管理。issue #6 [2.2.1]。
///
/// 独立為一個 controller（而非在 UsersController 上加子路由）是為了避免與 PR #38 (issue #3)
/// 即將建立的 UsersController 走 merge conflict。合併後可以重構為同一個 controller。
/// </summary>
[ApiController]
[Route("api/users/{userId:guid}/roles")]
public sealed class UserRolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserRolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 為指定使用者新增一組角色指派（複合角色）。
    /// 註意：本端點為「新增」，不會清除使用者現有的其他角色。要撤銷角色請使用
    /// <c>UserRole.Revoke</c>（由未來另一個端點暴露）。
    /// </summary>
    [HttpPut]
    [Authorize(Policy = Permissions.UsersAssignRoles)]
    public async Task<IActionResult> AssignRoles(
        Guid userId,
        [FromBody] AssignRolesRequest request,
        CancellationToken cancellationToken)
    {
        // AssignedByUserId 以未來能拿到 ICurrentUserService 後取代；目前留 null。
        var cmd = new AssignRolesToUserCommand(
            UserId: userId,
            RoleIds: request.RoleIds ?? Array.Empty<Guid>(),
            EffectiveFrom: request.EffectiveFrom,
            EffectiveTo: request.EffectiveTo,
            AssignedByUserId: null);
        await _mediator.Send(cmd, cancellationToken);
        return NoContent();
    }

    public sealed record AssignRolesRequest(
        IReadOnlyList<Guid>? RoleIds,
        DateTimeOffset? EffectiveFrom,
        DateTimeOffset? EffectiveTo);
}
