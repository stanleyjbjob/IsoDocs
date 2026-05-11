using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;
using IsoDocs.Domain.Identity;

namespace IsoDocs.Application.Users.Commands.InviteUser;

public sealed class InviteUserCommandHandler : ICommandHandler<InviteUserCommand, InviteUserResult>
{
    private readonly IUserRepository _userRepo;
    private readonly IGraphInvitationService _graphService;

    public InviteUserCommandHandler(IUserRepository userRepo, IGraphInvitationService graphService)
    {
        _userRepo = userRepo;
        _graphService = graphService;
    }

    public async Task<InviteUserResult> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        var inviter = await _userRepo.FindByAzureAdObjectIdAsync(request.InviterAzureAdObjectId, cancellationToken);
        if (inviter is null || !inviter.IsSystemAdmin)
            throw new DomainException("users.invite.not_admin", "只有系統管理者才可邀請成員。");

        var existing = await _userRepo.FindByEmailAsync(request.InviteeEmail, cancellationToken);
        if (existing is not null)
            throw new DomainException("users.invite.email_exists", $"Email {request.InviteeEmail} 已存在於系統中。");

        var role = await _userRepo.FindRoleByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            throw new DomainException("users.invite.role_not_found", $"角色 {request.RoleId} 不存在。");

        var invitationResult = await _graphService.InviteGuestAsync(
            request.InviteeEmail,
            request.InviteeDisplayName,
            request.InviteRedirectUrl,
            cancellationToken);

        var userId = Guid.NewGuid();
        var user = new User(
            id: userId,
            azureAdObjectId: invitationResult.InvitedUserObjectId,
            email: request.InviteeEmail,
            displayName: request.InviteeDisplayName);

        await _userRepo.AddUserAsync(user, cancellationToken);

        var userRole = new UserRole(
            id: Guid.NewGuid(),
            userId: userId,
            roleId: request.RoleId,
            effectiveFrom: DateTimeOffset.UtcNow,
            effectiveTo: null,
            assignedByUserId: inviter.Id);

        await _userRepo.AddUserRoleAsync(userRole, cancellationToken);
        await _userRepo.SaveChangesAsync(cancellationToken);

        return new InviteUserResult(userId, request.InviteeEmail, invitationResult.InviteRedeemUrl);
    }
}
