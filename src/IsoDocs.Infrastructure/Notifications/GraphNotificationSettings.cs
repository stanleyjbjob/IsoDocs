namespace IsoDocs.Infrastructure.Notifications;

/// <summary>
/// appsettings.json 中 "GraphNotification" 區段的強型別設定。
/// </summary>
public sealed class GraphNotificationSettings
{
    public const string SectionName = "GraphNotification";

    /// <summary>Azure AD 租戶 ID。</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>App Registration 的 Client ID。</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>App Registration 的 Client Secret。</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>發送 Email 所用的服務帳號 Azure AD Object ID（需有 Mail.Send 權限）。</summary>
    public string SenderUserId { get; set; } = string.Empty;

    /// <summary>發送 Teams 訊息所用的服務帳號 Azure AD Object ID（需有 Chat.Create 權限）。</summary>
    public string TeamsBotUserId { get; set; } = string.Empty;

    /// <summary>發送失敗後最大重試次數（不含首次）。</summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>首次重試等待毫秒數，後續以指數退避遞增。</summary>
    public int RetryDelayMs { get; set; } = 1000;
}
