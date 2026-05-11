namespace IsoDocs.Application.Auth;

/// <summary>
/// Auth / Identity 模組共用錯誤碼。前端會以此 code 對應到使用者訊息。
/// </summary>
public static class AuthErrorCodes
{
    /// <summary>Bearer Token 缺少 oid claim。Azure AD 應用程式註冊未開啟 "sign-in audience"-相關設定常見。</summary>
    public const string MissingObjectId = "AUTH/MISSING_OID";

    /// <summary>Token 中的 tid 與本系統設定的 TenantId 不符。</summary>
    public const string TenantMismatch = "AUTH/TENANT_MISMATCH";

    /// <summary>Azure AD ObjectId 對應的使用者已被本系統停用（離職等）。</summary>
    public const string UserDeactivated = "AUTH/USER_DEACTIVATED";
}
