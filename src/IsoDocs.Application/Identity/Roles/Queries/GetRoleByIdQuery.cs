using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;

namespace IsoDocs.Application.Identity.Roles.Queries;

/// <summary>
/// 依 Id 取得單一角色。對應 GET /api/roles/{id}。
/// </summary>
public sealed record GetRoleByIdQuery(Guid RoleId) : IQuery<RoleDto>;

public sealed class GetRoleByIdQueryHandler : IQueryHandler<GetRoleByIdQuery, RoleDto>
{
    private readonly IRoleRepository _roles;

    public GetRoleByIdQueryHandler(IRoleRepository roles)
    {
        _roles = roles;
    }

    public async Task<RoleDto> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await _roles.FindByIdAsync(request.RoleId, cancellationToken)
            ?? throw new DomainException(RoleErrorCodes.NotFound, $"找不到角色 {request.RoleId}。");
        return RoleDtoMapper.ToDto(role);
    }
}
