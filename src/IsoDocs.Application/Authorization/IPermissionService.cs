using System.Security.Claims;

namespace IsoDocs.Application.Authorization;

/// <summary>
/// 解析特定使用者實際擁有的權限集合。
///
/// 流程（issue #6 [2.2.1]）：
/// <list type="number">
///   <item>從 ClaimsPrincipal 取得 Azure AD oid → 找到本系統 User。</item>
///   <item>取出該 User 在「現在」生效的所有 UserRole。</item>
///   <item>合併每個 Role.PermissionsJson 解析後的權限碼，得到聯集。</item>
///   <item>若 User.IsSystemAdmin=true，自動視同擁有 <see cref="Permissions.AdminFullAccess"/>。</item>
/// </list>
///
/// PermissionAuthorizationHandler 會以此服務回答「使用者是否具有 X 權限」。
/// 實作應在請求生命週期內快取結果（避免同一請求多次查 DB）。
/// </summary>
public interface IPermissionService
{
    /// <summary>取得指定 <see cref="ClaimsPrincipal"/> 對應使用者目前生效的全部權限碼。</summary>
    Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);

    /// <summary>直接判斷該 principal 是否擁有指定權限。對外較常用的便利方法。</summary>
    Task<bool> HasPermissionAsync(
        ClaimsPrincipal principal,
        string permission,
        CancellationToken cancellationToken = default);
}
