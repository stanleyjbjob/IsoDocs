namespace IsoDocs.Application.Auth;

/// <summary>
/// /api/me 回傳的當前使用者資訊。前端 AuthContext 以此建立登入狀態快照。
/// </summary>
public sealed record CurrentUserDto(
    Guid Id,
    string AzureAdObjectId,
    string TenantId,
    string Email,
    string DisplayName,
    string? Department,
    string? JobTitle,
    bool IsActive,
    bool IsSystemAdmin,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Scopes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
