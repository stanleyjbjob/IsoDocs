using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;
using MediatR;

namespace IsoDocs.Application.Identity.Delegations.Commands;

/// <summary>
/// 撤銷指定代理設定。對應 DELETE /api/delegations/{id}。
/// RequesterUserId 由 Controller 從 ClaimsPrincipal 解析後注入，僅委託人本人或管理者可撤銷。
/// </summary>
public sealed record RevokeDelegationCommand(
    Guid DelegationId,
    Guid RequesterUserId,
    bool IsAdmin) : ICommand;

public sealed class RevokeDelegationCommandHandler : ICommandHandler<RevokeDelegationCommand>
{
    private readonly IDelegationRepository _delegations;

    public RevokeDelegationCommandHandler(IDelegationRepository delegations)
    {
        _delegations = delegations;
    }

    public async Task<Unit> Handle(RevokeDelegationCommand request, CancellationToken cancellationToken)
    {
        var delegation = await _delegations.GetByIdAsync(request.DelegationId, cancellationToken);
        if (delegation is null)
            throw new DomainException(DelegationErrorCodes.NotFound, "找不到指定的代理設定");

        if (!request.IsAdmin && delegation.DelegatorUserId != request.RequesterUserId)
            throw new DomainException(DelegationErrorCodes.NotOwner, "只有委託人本人或管理者可撤銷代理設定");

        if (delegation.IsRevoked)
            throw new DomainException(DelegationErrorCodes.AlreadyRevoked, "代理設定已撤銷");

        delegation.Revoke();
        await _delegations.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
