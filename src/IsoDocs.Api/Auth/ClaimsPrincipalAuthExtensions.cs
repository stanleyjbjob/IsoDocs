using System.Security.Claims;
using IsoDocs.Application.Auth;

namespace IsoDocs.Api.Auth;

/// <summary>
/// <see cref="ClaimsPrincipal"/> → <see cref="AzureAdUserPrincipal"/> 的封裝。
///
/// Microsoft.Identity.Web 預設會保留原始 claim 名稱（沒有 ClaimType mapping），
/// 所以這裡以原始 claim 名為主，並備援 .NET 傳統的 ClaimTypes.* URI。
/// </summary>
public static class ClaimsPrincipalAuthExtensions
{
    /// <summary>Azure AD object id (使用者在 tenant 中的唯一識別碼)。</summary>
    public const string OidClaim = "oid";

    /// <summary>Azure AD tenant id。</summary>
    public const string TidClaim = "tid";

    /// <summary>Azure AD scope claim（access token 才有，空格分隔多個）。</summary>
    public const string ScpClaim = "scp";

    private const string OidUri = "http://schemas.microsoft.com/identity/claims/objectidentifier";
    private const string TidUri = "http://schemas.microsoft.com/identity/claims/tenantid";
    private const string ScopeUri = "http://schemas.microsoft.com/identity/claims/scope";

    public static AzureAdUserPrincipal ToAzureAdUserPrincipal(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var oid = principal.FindFirst(OidClaim)?.Value
                  ?? principal.FindFirst(OidUri)?.Value
                  ?? string.Empty;
        var tid = principal.FindFirst(TidClaim)?.Value
                  ?? principal.FindFirst(TidUri)?.Value
                  ?? string.Empty;
        var email = principal.FindFirst("email")?.Value
                    ?? principal.FindFirst("preferred_username")?.Value
                    ?? principal.FindFirst(ClaimTypes.Email)?.Value
                    ?? principal.FindFirst("upn")?.Value
                    ?? principal.FindFirst(ClaimTypes.Upn)?.Value
                    ?? string.Empty;
        var name = principal.FindFirst("name")?.Value
                   ?? principal.FindFirst(ClaimTypes.Name)?.Value
                   ?? string.Empty;
        var department = principal.FindFirst("department")?.Value;
        var jobTitle = principal.FindFirst("jobTitle")?.Value;

        // Roles 在 Azure AD 以 "roles" claim 或 ClaimTypes.Role 呈現 (看 mapping 設定)。
        var rolesFromCustom = principal.FindAll("roles").Select(c => c.Value);
        var rolesFromBuiltin = principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
        var roles = rolesFromCustom.Concat(rolesFromBuiltin).Distinct(StringComparer.Ordinal).ToList();

        // Access token scope 以空格分隔多個 scope (e.g. "Cases.Read Cases.Write")
        var scpRaw = principal.FindFirst(ScpClaim)?.Value
                     ?? principal.FindFirst(ScopeUri)?.Value
                     ?? string.Empty;
        var scopes = scpRaw
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        return new AzureAdUserPrincipal(
            AzureAdObjectId: oid,
            TenantId: tid,
            Email: email,
            DisplayName: name,
            Department: department,
            JobTitle: jobTitle,
            Roles: roles,
            Scopes: scopes);
    }
}
