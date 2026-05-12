using IsoDocs.Application.Auth;
using IsoDocs.Application.Authorization;
using IsoDocs.Application.Identity.Delegations;
using IsoDocs.Application.Identity.Delegations.Commands;
using IsoDocs.Application.Identity.Delegations.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/delegations — 代理設定管理（issue #30 [2.3.2]）。
///
/// 一般使用者可設定、查詢、撤銷自己的代理；管理者可查詢/撤銷任何人的代理。
/// </summary>
[ApiController]
[Route("api/delegations")]
[Authorize]
public sealed class DelegationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserSyncService _userSync;

    public DelegationsController(IMediator mediator, IUserSyncService userSync)
    {
        _mediator = mediator;
        _userSync = userSync;
    }

    /// <summary>建立代理設定。委託人固定為當前登入使用者。</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.DelegationsWrite)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDelegationRequest request,
        CancellationToken cancellationToken)
    {
        var principal = User.ToAzureAdUserPrincipal();
        var currentUser = await _userSync.UpsertFromAzureAdAsync(principal, cancellationToken);

        var cmd = new CreateDelegationCommand(
            DelegatorUserId: currentUser.Id,
            DelegateUserId: request.DelegateUserId,
            StartAt: request.StartAt,
            EndAt: request.EndAt,
            Note: request.Note);

        var id = await _mediator.Send(cmd, cancellationToken);
        return CreatedAtAction(nameof(List), new { delegatorUserId = currentUser.Id }, new { id });
    }

    /// <summary>列出指定委託人的所有代理設定。未指定則列出當前使用者的設定。</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.DelegationsRead)]
    public async Task<ActionResult<IReadOnlyList<DelegationDto>>> List(
        [FromQuery] Guid? delegatorUserId,
        CancellationToken cancellationToken)
    {
        Guid targetUserId;
        if (delegatorUserId.HasValue)
        {
            targetUserId = delegatorUserId.Value;
        }
        else
        {
            var principal = User.ToAzureAdUserPrincipal();
            var currentUser = await _userSync.UpsertFromAzureAdAsync(principal, cancellationToken);
            targetUserId = currentUser.Id;
        }

        var result = await _mediator.Send(new ListDelegationsQuery(targetUserId), cancellationToken);
        return Ok(result);
    }

    /// <summary>撤銷指定代理設定。只有委託人本人或管理者可撤銷。</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.DelegationsWrite)]
    public async Task<IActionResult> Revoke(
        Guid id,
        CancellationToken cancellationToken)
    {
        var principal = User.ToAzureAdUserPrincipal();
        var currentUser = await _userSync.UpsertFromAzureAdAsync(principal, cancellationToken);

        var cmd = new RevokeDelegationCommand(
            DelegationId: id,
            RequesterUserId: currentUser.Id,
            IsAdmin: currentUser.IsSystemAdmin);

        await _mediator.Send(cmd, cancellationToken);
        return NoContent();
    }

    public sealed record CreateDelegationRequest(
        Guid DelegateUserId,
        DateTimeOffset StartAt,
        DateTimeOffset EndAt,
        string? Note);
}
