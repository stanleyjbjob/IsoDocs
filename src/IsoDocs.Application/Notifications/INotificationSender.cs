using IsoDocs.Domain.Communications;

namespace IsoDocs.Application.Notifications;

/// <summary>
/// 通知發送抽象（issue [6.1] 由 Microsoft Graph 實作，[6.3] 排程任務呼叫）。
/// </summary>
public interface INotificationSender
{
    Task SendAsync(Notification notification, CancellationToken cancellationToken = default);
}
