using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace IsoDocs.Infrastructure.Notifications;

/// <summary>
/// 透過 Microsoft Graph API 發送 Teams 1-on-1 訊息。
/// 需要 App Registration 具備 Chat.Create 與 ChatMessage.Send 應用程式權限。
/// </summary>
internal sealed class GraphTeamsNotificationService : ITeamsNotificationService
{
    private readonly GraphServiceClient _graphClient;
    private readonly GraphNotificationSettings _settings;
    private readonly ILogger<GraphTeamsNotificationService> _logger;

    public GraphTeamsNotificationService(
        GraphServiceClient graphClient,
        IOptions<GraphNotificationSettings> options,
        ILogger<GraphTeamsNotificationService> logger)
    {
        _graphClient = graphClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendMessageAsync(
        string recipientAzureAdObjectId,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        // 建立或取得服務帳號與收件人之間的 1-on-1 Chat
        var chat = await _graphClient.Chats.PostAsync(new Chat
        {
            ChatType = ChatType.OneOnOne,
            Members = new List<ConversationMember>
            {
                new AadUserConversationMember
                {
                    OdataType = "#microsoft.graph.aadUserConversationMember",
                    Roles = new List<string> { "owner" },
                    AdditionalData = new Dictionary<string, object>
                    {
                        {
                            "user@odata.bind",
                            $"https://graph.microsoft.com/v1.0/users('{_settings.TeamsBotUserId}')"
                        }
                    }
                },
                new AadUserConversationMember
                {
                    OdataType = "#microsoft.graph.aadUserConversationMember",
                    Roles = new List<string> { "owner" },
                    AdditionalData = new Dictionary<string, object>
                    {
                        {
                            "user@odata.bind",
                            $"https://graph.microsoft.com/v1.0/users('{recipientAzureAdObjectId}')"
                        }
                    }
                }
            }
        }, cancellationToken: cancellationToken);

        if (chat?.Id is null)
            throw new InvalidOperationException(
                $"Failed to create Teams chat with user {recipientAzureAdObjectId}.");

        await _graphClient.Chats[chat.Id].Messages.PostAsync(new ChatMessage
        {
            Body = new ItemBody
            {
                ContentType = BodyType.Html,
                Content = $"<b>{subject}</b><br/>{body}"
            }
        }, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Teams message sent to user {RecipientObjectId} via chat {ChatId}",
            recipientAzureAdObjectId, chat.Id);
    }
}
