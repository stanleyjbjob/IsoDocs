using System.Text.Json;
using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;

namespace IsoDocs.Application.Identity.Roles.Commands;

/// <summary>
/// 更新既有角色的名稱、描述、權限。對應 PUT /api/roles/{id}。
/// </summary>
public sealed record UpdateRoleCommand(
    Guid RoleId,
    string Name,
    string? Description,
    IReadOnlyList<string> Permissions) : ICommand<RoleDto>;

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("角色名稱必填")
            .MaximumLength(100);

        RuleFor(x => x.Description).MaximumLength(500);

        RuleFor(x => x.Permissions).NotNull();

        RuleForEach(x => x.Permissions)
            .Must(Authorization.Permissions.IsKnown)
            .WithMessage(p => $"未知的權限碼：{p}。");
    }
}

public sealed class UpdateRoleCommandHandler : ICommandHandler<UpdateRoleCommand, RoleDto>
{
    private readonly IRoleRepository _roles;

    public UpdateRoleCommandHandler(IRoleRepository roles)
    {
        _roles = roles;
    }

    public async Task<RoleDto> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roles.FindByIdAsync(request.RoleId, cancellationToken)
            ?? throw new DomainException(RoleErrorCodes.NotFound, $"找不到角色 {request.RoleId}。");

        // 同名衝突檢查（排除自己）
        if (!string.Equals(role.Name, request.Name, StringComparison.Ordinal))
        {
            var conflict = await _roles.FindByNameExcludingIdAsync(request.Name, request.RoleId, cancellationToken);
            if (conflict is not null)
            {
                throw new DomainException(RoleErrorCodes.NameDuplicate, $"角色名稱 '{request.Name}' 已存在。");
            }
        }

        var permissionsJson = JsonSerializer.Serialize(request.Permissions);
        role.Update(request.Name, request.Description, permissionsJson);

        await _roles.SaveChangesAsync(cancellationToken);
        return RoleDtoMapper.ToDto(role);
    }
}
