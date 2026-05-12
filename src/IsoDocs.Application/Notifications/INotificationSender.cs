namespace IsoDocs.Application.Notifications;

/// <summary>
/// 通知發送統一介面。由 Application 層呼叫，Infrastructure 層負責透過 Microsoft Graph 實際傳送。
/// </summary>
public interface INotificationSender
{
    /// <summary>依 <paramref name="request"/> 指定的通道發送通知並寫入持久化紀錄。</summary>
    Task SendAsync(NotificationRequest request, CancellationToken cancellationToken = default);
}
