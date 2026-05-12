using IsoDocs.Domain.Communications;

namespace IsoDocs.Application.Notifications;

/// <summary>
/// 傳遞給 <see cref="INotificationSender.SendAsync"/> 的通知請求參數。
/// </summary>
public sealed record NotificationRequest(
    Guid RecipientUserId,
    string RecipientEmail,
    string RecipientAzureAdObjectId,
    NotificationType Type,
    string Subject,
    string Body,
    Guid? CaseId = null,
    string? PayloadJson = null,
    NotificationChannel[]? Channels = null
);
