using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Identity;
using MediatR;

namespace IsoDocs.Application.Identity.Delegations.Commands;

/// <summary>
/// 建立新代理設定。對應 POST /api/delegations。
/// DelegatorUserId 由 Controller 從 ClaimsPrincipal 解析後注入。
/// </summary>
public sealed record CreateDelegationCommand(
    Guid DelegatorUserId,
    Guid DelegateUserId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string? Note) : ICommand<Guid>;

public sealed class CreateDelegationCommandValidator : AbstractValidator<CreateDelegationCommand>
{
    public CreateDelegationCommandValidator()
    {
        RuleFor(x => x.DelegatorUserId).NotEmpty();
        RuleFor(x => x.DelegateUserId).NotEmpty();
        RuleFor(x => x.DelegateUserId)
            .NotEqual(x => x.DelegatorUserId).WithMessage("不可指派自己為代理人");
        RuleFor(x => x.EndAt)
            .GreaterThan(x => x.StartAt).WithMessage("代理結束時間必須晚於開始時間");
        RuleFor(x => x.Note).MaximumLength(512).When(x => x.Note is not null);
    }
}

public sealed class CreateDelegationCommandHandler : ICommandHandler<CreateDelegationCommand, Guid>
{
    private readonly IDelegationRepository _delegations;

    public CreateDelegationCommandHandler(IDelegationRepository delegations)
    {
        _delegations = delegations;
    }

    public async Task<Guid> Handle(CreateDelegationCommand request, CancellationToken cancellationToken)
    {
        var delegation = new Delegation(
            id: Guid.NewGuid(),
            delegatorUserId: request.DelegatorUserId,
            delegateUserId: request.DelegateUserId,
            startAt: request.StartAt,
            endAt: request.EndAt,
            note: request.Note);

        await _delegations.AddAsync(delegation, cancellationToken);
        await _delegations.SaveChangesAsync(cancellationToken);

        return delegation.Id;
    }
}
