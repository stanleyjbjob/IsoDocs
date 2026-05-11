using System.Text.Json;
using IsoDocs.Domain.Identity;

namespace IsoDocs.Application.Identity.Roles;

/// <summary>
/// <see cref="Role"/> 與 <see cref="RoleDto"/> 之間的對應。集中於此一處解析 PermissionsJson。
/// </summary>
public static class RoleDtoMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static RoleDto ToDto(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        var permissions = ParsePermissions(role.PermissionsJson);
        return new RoleDto(
            Id: role.Id,
            Name: role.Name,
            Description: role.Description,
            Permissions: permissions,
            IsSystemRole: role.IsSystemRole,
            IsActive: role.IsActive,
            CreatedAt: role.CreatedAt,
            UpdatedAt: role.UpdatedAt);
    }

    /// <summary>解析 PermissionsJson（容錯：null/空字串/壞 JSON 都回空集合）。</summary>
    public static IReadOnlyList<string> ParsePermissions(string? permissionsJson)
    {
        if (string.IsNullOrWhiteSpace(permissionsJson))
        {
            return Array.Empty<string>();
        }
        try
        {
            return JsonSerializer.Deserialize<string[]>(permissionsJson, JsonOptions) ?? Array.Empty<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }
}
