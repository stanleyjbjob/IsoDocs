using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;
using MediatR;

namespace IsoDocs.Application.Identity.Roles.Commands;

/// <summary>
/// 停用或啟用角色。對應 DELETE /api/roles/{id}（停用）或 POST /api/roles/{id}/activate（啟用）。
/// 系統內建角色不可停用（Role.Deactivate 內部已防呆）。
/// </summary>
public sealed record SetRoleActiveCommand(Guid RoleId, bool IsActive) : ICommand;

public sealed class SetRoleActiveCommandHandler : ICommandHandler<SetRoleActiveCommand>
{
    private readonly IRoleRepository _roles;

    public SetRoleActiveCommandHandler(IRoleRepository roles)
    {
        _roles = roles;
    }

    public async Task<Unit> Handle(SetRoleActiveCommand request, CancellationToken cancellationToken)
    {
        var role = await _roles.FindByIdAsync(request.RoleId, cancellationToken)
            ?? throw new DomainException(RoleErrorCodes.NotFound, $"找不到角色 {request.RoleId}。");

        if (request.IsActive)
        {
            // 透過 Update 一次寫入「保持原值 + IsActive=true」並無公開方法。
            // Role 目前公開 Deactivate；Activate 由本輪一起補在 Domain 層（見下一個 commit）。
            RoleActivationHelper.Activate(role);
        }
        else
        {
            role.Deactivate(); // Deactivate 內部會對 IsSystemRole 防呆，拋 DomainException(role.system_role_cannot_deactivate)
        }

        await _roles.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
