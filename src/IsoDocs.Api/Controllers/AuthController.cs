using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/auth 端點。本系統為 SPA + API 架構，實際的 OIDC 流程由前端 MSAL.js 走 (issue #34)；
/// API 只負責 (a) 告訴前端需要哪些 Azure AD 設定才能登入、(b) 驗證后續請求上的 Bearer Token。
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 回傳 SPA 登入所需的 Azure AD 設定 (authority、clientId、scopes)。
    /// SPA 拿到後以 MSAL.js 走 authorization-code-with-PKCE flow。
    /// </summary>
    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        var instance = _configuration["AzureAd:Instance"]?.TrimEnd('/') ?? "https://login.microsoftonline.com";
        var tenantId = _configuration["AzureAd:TenantId"] ?? string.Empty;
        var clientId = _configuration["AzureAd:ClientId"] ?? string.Empty;
        var audience = _configuration["AzureAd:Audience"] ?? (string.IsNullOrWhiteSpace(clientId) ? string.Empty : $"api://{clientId}");

        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId))
        {
            return Problem(
                title: "Azure AD 未設定",
                detail: "請於 appsettings.json 或 user-secrets 裡填入 AzureAd:TenantId 與 AzureAd:ClientId。",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var defaultScope = $"{audience}/access_as_user".TrimStart('/');

        return Ok(new
        {
            authority = $"{instance}/{tenantId}/v2.0",
            tenantId,
            clientId,
            audience,
            scopes = new[] { defaultScope },
            // 提示前端該用哪個路由接住 auth response (仅供參考，實際在 Azure AD 應用程式註冊裡設)
            redirectUriHint = "http://localhost:5173/auth/callback"
        });
    }

    /// <summary>
    /// 提示前端熟 SPA 自行呼叫 MSAL.logoutRedirect()。有動作才走這裡，純為讓 SDK 留點 log。
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        return Ok(new
        {
            message = "已接收登出事件。請於 SPA 呼叫 MSAL.logoutRedirect() 清除所有 token cache。"
        });
    }
}
