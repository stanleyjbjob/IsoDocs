using IsoDocs.Application.Notifications;
using IsoDocs.Domain.Communications;
using Microsoft.Extensions.Logging;

namespace IsoDocs.Infrastructure.Notifications;

/// <summary>
/// 暫用實作：僅記錄 log，待 issue [6.1] 完成後由 MicrosoftGraphNotificationSender 取代。
/// </summary>
internal sealed class LoggingNotificationSender : INotificationSender
{
    private readonly ILogger<LoggingNotificationSender> _logger;

    public LoggingNotificationSender(ILogger<LoggingNotificationSender> logger)
        => _logger = logger;

    public Task SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[通知-暫用] Channel={Channel} Type={Type} RecipientUserId={RecipientUserId} Subject={Subject}",
            notification.Channel, notification.Type, notification.RecipientUserId, notification.Subject);
        return Task.CompletedTask;
    }
}
