using IsoDocs.Api.Auth;
using IsoDocs.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IsoDocs.Api.Controllers;

/// <summary>
/// /api/me — 當前使用者資訊。
///
/// 服務流程：
/// 1. ASP.NET Core auth pipeline 驗證 Bearer Token。
/// 2. 從 <see cref="ClaimsPrincipal"/> 解析出 <see cref="AzureAdUserPrincipal"/>。
/// 3. 呼叫 <see cref="IUserSyncService.UpsertFromAzureAdAsync"/> 同步 User 表。
/// 4. 若 User 在本系統被停用 (IsActive=false)，回 403 + AUTH/USER_DEACTIVATED。
/// 5. 否則回 <see cref="CurrentUserDto"/>。
/// </summary>
[ApiController]
[Route("api/me")]
[Authorize]
public class MeController : ControllerBase
{
    private readonly IUserSyncService _userSync;

    public MeController(IUserSyncService userSync)
    {
        _userSync = userSync;
    }

    [HttpGet]
    public async Task<ActionResult<CurrentUserDto>> Get(CancellationToken cancellationToken)
    {
        var principal = User.ToAzureAdUserPrincipal();

        if (!principal.IsAuthenticated)
        {
            // 理論上 [Authorize] 不會讓未認證請求走到這裡，但若 token 缺 oid claim 也該拍到這條路。
            return Unauthorized(new
            {
                code = AuthErrorCodes.MissingObjectId,
                message = "存取 Token 未含 oid claim，無法識別使用者。"
            });
        }

        var user = await _userSync.UpsertFromAzureAdAsync(principal, cancellationToken);

        if (!user.IsActive)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                code = AuthErrorCodes.UserDeactivated,
                message = "使用者帳號已被停用，無法使用本系統。如需重新啟用請洽管理者。"
            });
        }

        return Ok(new CurrentUserDto(
            Id: user.Id,
            AzureAdObjectId: user.AzureAdObjectId,
            TenantId: principal.TenantId,
            Email: user.Email,
            DisplayName: user.DisplayName,
            Department: user.Department,
            JobTitle: user.JobTitle,
            IsActive: user.IsActive,
            IsSystemAdmin: user.IsSystemAdmin,
            // 本 issue 只同步到這裡；UserRole/Role 顯示在 issue #6 [2.2.1] 裡補上。
            // 目前呈現 Token 裡出現的 roles claim（例如 App roles）作為零時資訊。
            Roles: principal.Roles,
            Scopes: principal.Scopes,
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt));
    }
}
