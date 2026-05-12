using IsoDocs.Application.Auth;
using IsoDocs.Application.Communications.Commands;
using IsoDocs.Application.Communications.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/notifications — 通知中心。issue #24 [6.2]。
/// 支援列出通知、標記已讀、全部標記已讀。
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserSyncService _userSync;

    public NotificationsController(IMediator mediator, IUserSyncService userSync)
    {
        _mediator = mediator;
        _userSync = userSync;
    }

    /// <summary>
    /// 列出目前使用者的通知。支援未讀篩選與分頁。
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ListNotificationsResult>> List(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        var result = await _mediator.Send(
            new ListNotificationsQuery(user.Id, unreadOnly, page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 將指定通知標記為已讀。
    /// </summary>
    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        await _mediator.Send(new MarkNotificationReadCommand(id, user.Id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// 將目前使用者所有未讀通知一次標記為已讀。
    /// </summary>
    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        await _mediator.Send(new MarkAllNotificationsReadCommand(user.Id), cancellationToken);
        return NoContent();
    }

    private async Task<IsoDocs.Domain.Identity.User> GetCurrentUserAsync(
        CancellationToken cancellationToken)
    {
        var principal = User.ToAzureAdUserPrincipal();
        return await _userSync.UpsertFromAzureAdAsync(principal, cancellationToken);
    }
}
