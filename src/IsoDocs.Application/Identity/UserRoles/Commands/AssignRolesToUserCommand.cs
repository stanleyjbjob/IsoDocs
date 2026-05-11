using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Application.Identity.Roles;
using IsoDocs.Domain.Common;
using IsoDocs.Domain.Identity;
using MediatR;

namespace IsoDocs.Application.Identity.UserRoles.Commands;

/// <summary>
/// 為使用者新增一組角色指派（複合角色）。對應 PUT /api/users/{userId}/roles。
///
/// 設計策略：
///   - 「新增」而非「全量替換」：避免不小心把使用者所有角色都洗掉。
///   - <see cref="EffectiveFrom"/> 與 <see cref="EffectiveTo"/> 控制有效期，未提供則永久有效。
///   - <see cref="AssignedByUserId"/> 留審計痕跡，由 Controller 從 ClaimsPrincipal 解析後注入。
/// </summary>
public sealed record AssignRolesToUserCommand(
    Guid UserId,
    IReadOnlyList<Guid> RoleIds,
    DateTimeOffset? EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    Guid? AssignedByUserId) : ICommand;

public sealed class AssignRolesToUserCommandValidator : AbstractValidator<AssignRolesToUserCommand>
{
    public AssignRolesToUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleIds)
            .NotNull()
            .Must(ids => ids.Count > 0).WithMessage("必須至少指派一個角色")
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("RoleIds 不可重複");

        RuleFor(x => x)
            .Must(c => c.EffectiveTo is null || c.EffectiveFrom is null || c.EffectiveTo > c.EffectiveFrom)
            .WithMessage("EffectiveTo 必須晚於 EffectiveFrom");
    }
}

public sealed class AssignRolesToUserCommandHandler : ICommandHandler<AssignRolesToUserCommand>
{
    private readonly IRoleRepository _roles;
    private readonly IUserRoleRepository _userRoles;

    public AssignRolesToUserCommandHandler(IRoleRepository roles, IUserRoleRepository userRoles)
    {
        _roles = roles;
        _userRoles = userRoles;
    }

    public async Task<Unit> Handle(AssignRolesToUserCommand request, CancellationToken cancellationToken)
    {
        // 確認所有 RoleId 都存在且未停用
        var found = await _roles.ListByIdsAsync(request.RoleIds, cancellationToken);
        var foundIds = found.Select(r => r.Id).ToHashSet();
        var missing = request.RoleIds.Where(id => !foundIds.Contains(id)).ToList();
        if (missing.Count > 0)
        {
            throw new DomainException(
                RoleErrorCodes.AssignmentFailed,
                $"以下角色不存在或已停用：{string.Join(\", \", missing)}");
        }
        var inactive = found.Where(r => !r.IsActive).Select(r => r.Id).ToList();
        if (inactive.Count > 0)
        {
            throw new DomainException(
                RoleErrorCodes.AssignmentFailed,
                $"以下角色已停用，無法指派：{string.Join(\", \", inactive)}");
        }

        var effectiveFrom = request.EffectiveFrom ?? DateTimeOffset.UtcNow;
        var assignments = request.RoleIds.Select(roleId => new UserRole(
            id: Guid.NewGuid(),
            userId: request.UserId,
            roleId: roleId,
            effectiveFrom: effectiveFrom,
            effectiveTo: request.EffectiveTo,
            assignedByUserId: request.AssignedByUserId)).ToList();

        await _userRoles.AddRangeAsync(assignments, cancellationToken);
        await _userRoles.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
