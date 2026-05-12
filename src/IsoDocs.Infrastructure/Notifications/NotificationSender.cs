using IsoDocs.Application.Notifications;
using IsoDocs.Domain.Communications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IsoDocs.Infrastructure.Notifications;

/// <summary>
/// <see cref="INotificationSender"/> 實作。
/// 依指定通道發送通知，失敗時以指數退避重試，全程寫入 <see cref="Notification"/> 紀錄。
/// </summary>
internal sealed class NotificationSender : INotificationSender
{
    private readonly ITeamsNotificationService _teamsService;
    private readonly IEmailNotificationService _emailService;
    private readonly INotificationRepository _repository;
    private readonly GraphNotificationSettings _settings;
    private readonly ILogger<NotificationSender> _logger;

    public NotificationSender(
        ITeamsNotificationService teamsService,
        IEmailNotificationService emailService,
        INotificationRepository repository,
        IOptions<GraphNotificationSettings> options,
        ILogger<NotificationSender> logger)
    {
        _teamsService = teamsService;
        _emailService = emailService;
        _repository = repository;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        var channels = request.Channels
            ?? new[] { NotificationChannel.Teams, NotificationChannel.Email };

        foreach (var channel in channels)
        {
            var notification = new Notification(
                id: Guid.NewGuid(),
                recipientUserId: request.RecipientUserId,
                type: request.Type,
                channel: channel,
                subject: request.Subject,
                body: request.Body,
                caseId: request.CaseId,
                payloadJson: request.PayloadJson);

            await _repository.AddAsync(notification, cancellationToken);
            await SendWithRetryAsync(notification, request, channel, cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);
    }

    private async Task SendWithRetryAsync(
        Notification notification,
        NotificationRequest request,
        NotificationChannel channel,
        CancellationToken cancellationToken)
    {
        var maxRetries = _settings.MaxRetryCount;
        var baseDelayMs = _settings.RetryDelayMs;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                await DispatchAsync(request, channel, cancellationToken);
                notification.MarkSent();
                return;
            }
            catch (Exception ex) when (attempt < maxRetries && !cancellationToken.IsCancellationRequested)
            {
                notification.MarkFailed(ex.Message);
                var delayMs = baseDelayMs * (int)Math.Pow(2, attempt);
                _logger.LogWarning(ex,
                    "通知發送失敗（通道: {Channel}，第 {Attempt}/{Max} 次），{Delay}ms 後重試",
                    channel, attempt + 1, maxRetries + 1, delayMs);
                await Task.Delay(delayMs, cancellationToken);
            }
            catch (Exception ex)
            {
                notification.MarkFailed(ex.Message);
                _logger.LogError(ex,
                    "通知發送失敗（通道: {Channel}），已達最大重試次數 {Max}",
                    channel, maxRetries + 1);
            }
        }
    }

    private Task DispatchAsync(
        NotificationRequest request,
        NotificationChannel channel,
        CancellationToken cancellationToken) => channel switch
    {
        NotificationChannel.Teams =>
            _teamsService.SendMessageAsync(
                request.RecipientAzureAdObjectId, request.Subject, request.Body, cancellationToken),
        NotificationChannel.Email =>
            _emailService.SendEmailAsync(
                request.RecipientEmail, request.Subject, request.Body, cancellationToken),
        _ => Task.CompletedTask
    };
}
