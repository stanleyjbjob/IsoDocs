using System.Security.Claims;
using IsoDocs.Application.Authorization;
using IsoDocs.Application.Auth;
using IsoDocs.Application.Identity.Roles;
using IsoDocs.Application.Identity.UserRoles;
using IsoDocs.Domain.Identity;
using IsoDocs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IsoDocs.Infrastructure.Authorization;

/// <summary>
/// <see cref="IPermissionService"/> 的 EF Core 實作。
///
/// 流程：
/// <list type="number">
///   <item>從 ClaimsPrincipal 解析出 AzureAd ObjectId（沿用 ClaimsPrincipalAuthExtensions）。</item>
///   <item>用 oid 查出本系統 User；若不存在或被停用視同無權限。</item>
///   <item>User.IsSystemAdmin=true 直接給 AdminFullAccess + 其他全部已知權限。</item>
///   <item>否則拉所有目前生效的 UserRole，IN 一次查回 Role 集合，合併 Role.PermissionsJson。</item>
/// </list>
///
/// 這裡刻意維持 Scoped 同 DbContext；快取交由 PermissionAuthorizationHandler 在
/// HttpContext.Items 做 per-request 暫存（避免同一個 request 內多次掃 DB）。
/// </summary>
internal sealed class PermissionService : IPermissionService
{
    private readonly IsoDocsDbContext _dbContext;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(IsoDocsDbContext dbContext, ILogger<PermissionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);
        var azureAd = principal.ToAzureAdUserPrincipal();
        if (!azureAd.IsAuthenticated)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.AzureAdObjectId == azureAd.AzureAdObjectId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        // 系統管理者 → 自動給予全部已知權限（含 AdminFullAccess 萬用旁路）
        if (user.IsSystemAdmin)
        {
            return new HashSet<string>(Permissions.All, StringComparer.Ordinal);
        }

        // 一般使用者：撈生效中的 UserRole，再 IN 撈 Role.PermissionsJson
        var now = DateTimeOffset.UtcNow;
        var effectiveRoleIds = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == user.Id
                         && ur.EffectiveFrom <= now
                         && (ur.EffectiveTo == null || ur.EffectiveTo > now))
            .Select(ur => ur.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (effectiveRoleIds.Count == 0)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Where(r => effectiveRoleIds.Contains(r.Id) && r.IsActive)
            .Select(r => r.PermissionsJson)
            .ToListAsync(cancellationToken);

        var permissions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var json in roles)
        {
            foreach (var p in RoleDtoMapper.ParsePermissions(json))
            {
                permissions.Add(p);
            }
        }

        if (permissions.Count == 0)
        {
            _logger.LogDebug(
                "使用者 {AzureAdObjectId} 雖有 {RoleCount} 個生效角色，但 PermissionsJson 解析後總和為空。",
                azureAd.AzureAdObjectId, effectiveRoleIds.Count);
        }

        return permissions;
    }

    public async Task<bool> HasPermissionAsync(
        ClaimsPrincipal principal,
        string permission,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }
        var permissions = await GetEffectivePermissionsAsync(principal, cancellationToken);

        // 持有 AdminFullAccess 視同擁有所有權限（萬用旁路；與 IsSystemAdmin 配合一致）
        return permissions.Contains(Permissions.AdminFullAccess)
               || permissions.Contains(permission);
    }
}
