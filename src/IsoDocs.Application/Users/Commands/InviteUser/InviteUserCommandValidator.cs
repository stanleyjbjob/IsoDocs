using FluentValidation;

namespace IsoDocs.Application.Users.Commands.InviteUser;

public class InviteUserCommandValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserCommandValidator()
    {
        RuleFor(x => x.InviterAzureAdObjectId).NotEmpty();
        RuleFor(x => x.InviteeEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.InviteeDisplayName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.InviteRedirectUrl).NotEmpty();
    }
}
