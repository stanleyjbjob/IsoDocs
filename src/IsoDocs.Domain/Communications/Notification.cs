using IsoDocs.Domain.Common;

namespace IsoDocs.Domain.Communications;

/// <summary>
/// 通知紀錄（issue [6.x]）。記錄發送狀態、送達通道，並提供前端未讀清單查詢。
/// </summary>
public class Notification : Entity<Guid>, IAggregateRoot
{
    public Guid RecipientUserId { get; protected set; }
    public Guid? CaseId { get; protected set; }
    public NotificationType Type { get; protected set; }
    public NotificationChannel Channel { get; protected set; }
    public string Subject { get; protected set; } = string.Empty;
    public string Body { get; protected set; } = string.Empty;
    public string? PayloadJson { get; protected set; }
    public DateTimeOffset? SentAt { get; protected set; }
    public bool IsRead { get; protected set; }
    public DateTimeOffset? ReadAt { get; protected set; }
    public int RetryCount { get; protected set; }
    public string? LastError { get; protected set; }

    private Notification() { }

    public Notification(Guid id, Guid recipientUserId, NotificationType type, NotificationChannel channel,
        string subject, string body, Guid? caseId, string? payloadJson)
    {
        Id = id;
        RecipientUserId = recipientUserId;
        Type = type;
        Channel = channel;
        Subject = subject;
        Body = body;
        CaseId = caseId;
        PayloadJson = payloadJson;
    }

    public void MarkSent()
    {
        SentAt = DateTimeOffset.UtcNow;
        UpdatedAt = SentAt;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        RetryCount += 1;
        LastError = error;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkRead()
    {
        IsRead = true;
        ReadAt = DateTimeOffset.UtcNow;
        UpdatedAt = ReadAt;
    }
}

public enum NotificationType
{
    NodeAssigned = 1,        // 節點流轉到自己
    CaseStatusChanged = 2,   // 發起案件狀態變更
    NewComment = 3,          // 案件新留言
    Voided = 4,              // 主單作廢
    SubprocessVoided = 5,    // 子流程獨立作廢
    Overdue = 6,             // 逾期稽催
    Custom = 99
}

public enum NotificationChannel
{
    InApp = 1,
    Email = 2,
    Teams = 3
}
