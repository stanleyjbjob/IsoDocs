namespace IsoDocs.Infrastructure.Notifications;

internal interface ITeamsNotificationService
{
    Task SendMessageAsync(
        string recipientAzureAdObjectId,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
