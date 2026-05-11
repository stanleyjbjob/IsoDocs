using System.Text.Json;
using FluentValidation;
using IsoDocs.Application.Authorization;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;
using IsoDocs.Domain.Identity;

namespace IsoDocs.Application.Identity.Roles.Commands;

/// <summary>
/// 建立新角色。對應 POST /api/roles 端點。
/// </summary>
public sealed record CreateRoleCommand(
    string Name,
    string? Description,
    IReadOnlyList<string> Permissions) : ICommand<RoleDto>;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("角色名稱必填")
            .MaximumLength(100).WithMessage("角色名稱不可超過 100 字");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("描述不可超過 500 字");

        RuleFor(x => x.Permissions)
            .NotNull().WithMessage("Permissions 不可為 null（空陣列代表無權限）");

        RuleForEach(x => x.Permissions)
            .Must(Authorization.Permissions.IsKnown)
            .WithMessage("未知的權限碼：{PropertyValue}。");
    }
}

public sealed class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, RoleDto>
{
    private readonly IRoleRepository _roles;

    public CreateRoleCommandHandler(IRoleRepository roles)
    {
        _roles = roles;
    }

    public async Task<RoleDto> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var duplicate = await _roles.FindByNameAsync(request.Name, cancellationToken);
        if (duplicate is not null)
        {
            throw new DomainException(RoleErrorCodes.NameDuplicate, $"角色名稱 '{request.Name}' 已存在。");
        }

        var permissionsJson = JsonSerializer.Serialize(request.Permissions);
        var role = new Role(Guid.NewGuid(), request.Name, permissionsJson, isSystemRole: false);
        role.Update(request.Name, request.Description, permissionsJson);

        await _roles.AddAsync(role, cancellationToken);
        await _roles.SaveChangesAsync(cancellationToken);

        return RoleDtoMapper.ToDto(role);
    }
}
