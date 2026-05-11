using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IsoDocs.Api.IntegrationTests;

/// <summary>
/// 整合測試專用 <see cref="AuthenticationHandler{TOptions}"/> — 取代正式的 Microsoft.Identity.Web。
///
/// 設計：直接從 <c>Authorization: Test &lt;encoded-claims&gt;</c> header 還原 claims，
/// 避開「需要真實 Azure AD JWKs 才能驗 token 簽章」的限制。
///
/// 用法：
/// <code>
/// client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
///     "Test",
///     TestAuthHandler.EncodeClaims(new[] { new Claim("oid", "..."), new Claim("name", "Alice") }));
/// </code>
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var headerValue = authHeader.ToString();
        if (string.IsNullOrEmpty(headerValue) ||
            !headerValue.StartsWith("Test ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var encoded = headerValue.Substring("Test ".Length);
        var claims = DecodeClaims(encoded).ToList();

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>
    /// 將 claims 序列化為 header value：<c>type1=value1,type2=value2</c>，type/value 都做 URL encode。
    /// </summary>
    public static string EncodeClaims(IEnumerable<Claim> claims)
    {
        var pairs = claims.Select(c => $"{Uri.EscapeDataString(c.Type)}={Uri.EscapeDataString(c.Value)}");
        return string.Join(",", pairs);
    }

    public static IEnumerable<Claim> DecodeClaims(string encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
        {
            yield break;
        }

        foreach (var pair in encoded.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = pair.IndexOf('=');
            if (idx <= 0)
            {
                continue;
            }
            var type = Uri.UnescapeDataString(pair.Substring(0, idx));
            var value = Uri.UnescapeDataString(pair.Substring(idx + 1));
            yield return new Claim(type, value);
        }
    }
}
