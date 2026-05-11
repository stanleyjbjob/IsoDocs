using IsoDocs.Application.Common.Messaging;

namespace IsoDocs.Application.Identity.Roles.Queries;

/// <summary>
/// 列出全部角色。對應 GET /api/roles。
/// </summary>
/// <param name="IncludeInactive">是否包含已停用角色，預設 true（前端管理介面需看全部）。</param>
public sealed record ListRolesQuery(bool IncludeInactive = true) : IQuery<IReadOnlyList<RoleDto>>;

public sealed class ListRolesQueryHandler : IQueryHandler<ListRolesQuery, IReadOnlyList<RoleDto>>
{
    private readonly IRoleRepository _roles;

    public ListRolesQueryHandler(IRoleRepository roles)
    {
        _roles = roles;
    }

    public async Task<IReadOnlyList<RoleDto>> Handle(ListRolesQuery request, CancellationToken cancellationToken)
    {
        var all = await _roles.ListAsync(cancellationToken);
        var filtered = request.IncludeInactive
            ? all
            : all.Where(r => r.IsActive).ToList();
        return filtered.Select(RoleDtoMapper.ToDto).ToList();
    }
}
