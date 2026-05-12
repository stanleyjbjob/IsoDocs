namespace IsoDocs.Infrastructure.Notifications;

internal interface IEmailNotificationService
{
    Task SendEmailAsync(
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
