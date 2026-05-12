namespace IsoDocs.Application.Communications;

/// <summary>
/// 通知資料傳輸物件。
/// </summary>
public sealed record NotificationDto(
    Guid Id,
    Guid RecipientUserId,
    Guid? CaseId,
    string Type,
    string Channel,
    string Subject,
    string Body,
    bool IsRead,
    DateTimeOffset? ReadAt,
    DateTimeOffset? SentAt,
    DateTimeOffset CreatedAt);
