namespace IsoDocs.Application.Auth;

/// <summary>
/// 從 Azure AD / Entra ID 的 Bearer Token claims 解析後的使用者主體（principal）。
///
/// API 層（IsoDocs.Api）會在 Authorize 後呼叫 ClaimsPrincipalExtensions.ToAzureAdUserPrincipal()
/// 產生此物件，再交給 <see cref="IUserSyncService"/> 做 upsert。
/// </summary>
/// <param name="AzureAdObjectId">
/// Azure AD object id（claim 名稱：<c>oid</c> 或 "http://schemas.microsoft.com/identity/claims/objectidentifier"）。
/// 跨應用穩定，作為本系統 <see cref="IsoDocs.Domain.Identity.User.AzureAdObjectId"/> 的識別鍵。
/// </param>
/// <param name="TenantId">Azure AD tenant id（claim：<c>tid</c>）。多租戶環境用於檢查發行 tenant。</param>
/// <param name="Email">使用者電子郵件，優先序：<c>email</c> → <c>preferred_username</c> → <c>upn</c>。</param>
/// <param name="DisplayName">使用者顯示名稱（claim：<c>name</c>）。</param>
/// <param name="Department">部門（claim：<c>department</c>，需於 Azure AD 應用程式 token configuration 加入 optional claim）。</param>
/// <param name="JobTitle">職稱（claim：<c>jobTitle</c>，同上 optional claim）。</param>
/// <param name="Roles">App roles（claim：<c>roles</c>），對應自訂角色設定。</param>
/// <param name="Scopes">Access token scope（claim：<c>scp</c>），用於授權檢查。</param>
public sealed record AzureAdUserPrincipal(
    string AzureAdObjectId,
    string TenantId,
    string Email,
    string DisplayName,
    string? Department,
    string? JobTitle,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Scopes)
{
    public static AzureAdUserPrincipal Empty { get; } = new(
        AzureAdObjectId: string.Empty,
        TenantId: string.Empty,
        Email: string.Empty,
        DisplayName: string.Empty,
        Department: null,
        JobTitle: null,
        Roles: Array.Empty<string>(),
        Scopes: Array.Empty<string>());

    /// <summary>是否含 Azure AD ObjectId（最低限度的有效性檢查）。</summary>
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AzureAdObjectId);
}
