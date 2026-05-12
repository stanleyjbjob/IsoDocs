using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;

namespace IsoDocs.Infrastructure.Notifications;

/// <summary>
/// 透過 Microsoft Graph API 使用服務帳號發送 Outlook Email。
/// 需要 App Registration 具備 Mail.Send 應用程式權限。
/// </summary>
internal sealed class GraphEmailNotificationService : IEmailNotificationService
{
    private readonly GraphServiceClient _graphClient;
    private readonly GraphNotificationSettings _settings;
    private readonly ILogger<GraphEmailNotificationService> _logger;

    public GraphEmailNotificationService(
        GraphServiceClient graphClient,
        IOptions<GraphNotificationSettings> options,
        ILogger<GraphEmailNotificationService> logger)
    {
        _graphClient = graphClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string recipientEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        await _graphClient.Users[_settings.SenderUserId].SendMail.PostAsync(
            new SendMailPostRequestBody
            {
                Message = new Message
                {
                    Subject = subject,
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Html,
                        Content = body
                    },
                    ToRecipients = new List<Recipient>
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress { Address = recipientEmail }
                        }
                    }
                },
                SaveToSentItems = false
            },
            cancellationToken: cancellationToken);

        _logger.LogInformation("Email sent to {RecipientEmail} via Microsoft Graph", recipientEmail);
    }
}
