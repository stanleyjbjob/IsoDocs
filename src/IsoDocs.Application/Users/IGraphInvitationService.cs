namespace IsoDocs.Application.Users;

public interface IGraphInvitationService
{
    Task<GraphInvitationResult> InviteGuestAsync(
        string email,
        string displayName,
        string inviteRedirectUrl,
        CancellationToken cancellationToken = default);
}

public record GraphInvitationResult(string InvitedUserObjectId, string InviteRedeemUrl);
