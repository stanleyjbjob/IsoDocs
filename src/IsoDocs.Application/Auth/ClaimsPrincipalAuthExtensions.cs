using System.Security.Claims;

namespace IsoDocs.Application.Auth;

/// <summary>
/// <see cref="ClaimsPrincipal"/> → <see cref="AzureAdUserPrincipal"/> 的映射。
///
/// Microsoft.Identity.Web 預設保留原始 claim 名稱（未作 ClaimType mapping），
/// 所以這裡以原始 claim 名為主，並備援 .NET 傳統的 ClaimTypes.* URI。
///
/// 這個 extension 位在 Application 層是為了讓 IsoDocs.Application.UnitTests 不需
/// 反向參照 Web SDK。System.Security.Claims 是 BCL，Application 可以直接依賴。
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

        var rolesFromCustom = principal.FindAll("roles").Select(c => c.Value);
        var rolesFromBuiltin = principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
        var roles = rolesFromCustom.Concat(rolesFromBuiltin)
            .Distinct(StringComparer.Ordinal)
            .ToList();

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
