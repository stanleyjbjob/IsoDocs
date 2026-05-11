namespace IsoDocs.Application.Identity.Roles;

/// <summary>
/// 角色資料傳輸物件。供 API 回傳，前端管理介面也用此 schema。
/// </summary>
public sealed record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<string> Permissions,
    bool IsSystemRole,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
