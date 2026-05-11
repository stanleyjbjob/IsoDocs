using IsoDocs.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace IsoDocs.Api.Authorization;

/// <summary>
/// <see cref="PermissionRequirement"/> 的驗證處理器。
///
/// 透過注入的 <see cref="IPermissionService"/> 取得當前使用者的有效權限集合，
/// 全序列比對 <see cref="PermissionRequirement.Permission"/>。
///
/// per-request 快取：同一 HttpContext 本來就會讓 IPermissionService 查同一個使用者多次，
/// 但為了不依賴 IMemoryCache、也不動 IPermissionService 實作，這裡直接在 HttpContext.Items
/// 做 per-request 暫存，避免同一 request 內多次 [Authorize(Policy=...)] 重複查 DB。
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private const string CacheKey = "__IsoDocs.EffectivePermissions";

    private readonly IPermissionService _permissions;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        IPermissionService permissions,
        IHttpContextAccessor httpContext,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _permissions = permissions;
        _httpContext = httpContext;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return; // FallbackPolicy.RequireAuthenticatedUser 本來就會抵 401
        }

        var cached = TryGetCached();
        IReadOnlySet<string> effective;
        if (cached is not null)
        {
            effective = cached;
        }
        else
        {
            effective = await _permissions.GetEffectivePermissionsAsync(context.User);
            StoreCached(effective);
        }

        // AdminFullAccess 視同擁有所有權限（與 IPermissionService.HasPermissionAsync 一致）
        if (effective.Contains(Permissions.AdminFullAccess) || effective.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogInformation(
                "Authorization failed: principal lacks permission {Permission} (has {Count} permissions)",
                requirement.Permission, effective.Count);
        }
    }

    private IReadOnlySet<string>? TryGetCached()
    {
        var ctx = _httpContext.HttpContext;
        if (ctx is null) return null;
        if (ctx.Items.TryGetValue(CacheKey, out var raw) && raw is IReadOnlySet<string> set)
        {
            return set;
        }
        return null;
    }

    private void StoreCached(IReadOnlySet<string> set)
    {
        var ctx = _httpContext.HttpContext;
        if (ctx is null) return;
        ctx.Items[CacheKey] = set;
    }
}
