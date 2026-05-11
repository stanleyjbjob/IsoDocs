using IsoDocs.Domain.Identity;

namespace IsoDocs.Application.Auth;

/// <summary>
/// Azure AD / Entra ID 使用者同步服務。
///
/// 用途：
/// 1. 每次成功通過 Bearer Token 驗證後，於 /api/me 第一次呼叫時 upsert <see cref="User"/>；
///    確保 Azure AD 上新增的使用者會自動同步到 IsoDocs（無需管理者額外開戶）。
/// 2. 提供 <see cref="DeactivateByAzureObjectIdAsync"/> 給未來「離職人員 Azure AD 失效自動禁用」
///    的 Microsoft Graph + Hangfire 排程（對應 issue #22 / #23）呼叫。
/// </summary>
public interface IUserSyncService
{
    /// <summary>
    /// 依 <paramref name="principal"/>（從 Bearer Token claims 解析）upsert 對應的 <see cref="User"/>。
    /// 已存在 → 更新 profile（email/displayName/department/jobTitle）並確保 IsActive=true。
    /// 不存在 → 以 <see cref="User"/> 公開建構式建立新使用者。
    /// </summary>
    /// <returns>upsert 後的 <see cref="User"/> 聚合根。</returns>
    Task<User> UpsertFromAzureAdAsync(
        AzureAdUserPrincipal principal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 依 Azure AD ObjectId 將使用者停用（IsActive=false）。
    /// 找不到對應 User 時回傳 <c>false</c>，不丟例外（離職同步是 fire-and-forget 流程）。
    /// </summary>
    Task<bool> DeactivateByAzureObjectIdAsync(
        string azureAdObjectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 依 Azure AD ObjectId 重新啟用使用者（IsActive=true）。提供管理者手動補救 / 重新邀請後使用。
    /// </summary>
    Task<bool> ReactivateByAzureObjectIdAsync(
        string azureAdObjectId,
        CancellationToken cancellationToken = default);
}
