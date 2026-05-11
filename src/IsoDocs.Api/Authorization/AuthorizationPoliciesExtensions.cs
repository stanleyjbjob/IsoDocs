using IsoDocs.Application.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace IsoDocs.Api.Authorization;

/// <summary>
/// 一次性註冊本系統中「每個權限碼一條 Policy」的便利方法。Policy 名稱 = 權限碼本身。
///
/// 使用：
/// <code>
/// services.AddAuthorization(opts =&gt;
/// {
///     opts.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
///     opts.AddIsoDocsPermissionPolicies();
/// });
/// </code>
/// </summary>
public static class AuthorizationPoliciesExtensions
{
    public static AuthorizationOptions AddIsoDocsPermissionPolicies(this AuthorizationOptions options)
    {
        foreach (var permission in Permissions.All)
        {
            options.AddPolicy(permission, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new PermissionRequirement(permission));
            });
        }
        return options;
    }
}
