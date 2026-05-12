using Hangfire.Dashboard;
using IsoDocs.Application.Authorization;

namespace IsoDocs.Api.Hangfire;

/// <summary>
/// Hangfire Dashboard 授權過濾器：僅允許擁有 admin.full_access 權限的已認證使用者存取。
/// </summary>
internal sealed class AdminDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
            return false;

        return httpContext.User.HasClaim(
            c => c.Type == "permission" && c.Value == Permissions.AdminFullAccess);
    }
}
