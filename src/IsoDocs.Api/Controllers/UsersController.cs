using IsoDocs.Application.Auth;
using IsoDocs.Application.Users.Commands.InviteUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// 管理者邀請成員加入系統並指派角色。
    /// 邀請者須為 IsSystemAdmin=true，否則回 422。
    /// 透過 Microsoft Graph 發送邀請 Email，並同步建立 User + UserRole 記錄。
    /// </summary>
    [HttpPost("invite")]
    public async Task<ActionResult<InviteUserResult>> Invite(
        [FromBody] InviteUserRequest request,
        CancellationToken cancellationToken)
    {
        var principal = User.ToAzureAdUserPrincipal();
        if (!principal.IsAuthenticated)
            return Unauthorized();

        var redirectUrl = request.InviteRedirectUrl
            ?? $"{Request.Scheme}://{Request.Host}";

        var command = new InviteUserCommand(
            InviterAzureAdObjectId: principal.AzureAdObjectId,
            InviteeEmail: request.Email,
            InviteeDisplayName: request.DisplayName,
            RoleId: request.RoleId,
            InviteRedirectUrl: redirectUrl);

        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }
}

public record InviteUserRequest(
    string Email,
    string DisplayName,
    Guid RoleId,
    string? InviteRedirectUrl);
