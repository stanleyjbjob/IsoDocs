using IsoDocs.Application.Common.Messaging;

namespace IsoDocs.Application.Users.Commands.InviteUser;

public record InviteUserCommand(
    string InviterAzureAdObjectId,
    string InviteeEmail,
    string InviteeDisplayName,
    Guid RoleId,
    string InviteRedirectUrl) : ICommand<InviteUserResult>;

public record InviteUserResult(Guid UserId, string Email, string InviteRedeemUrl);
