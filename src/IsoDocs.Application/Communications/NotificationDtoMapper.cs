using IsoDocs.Domain.Communications;

namespace IsoDocs.Application.Communications;

public static class NotificationDtoMapper
{
    public static NotificationDto ToDto(Notification n) =>
        new(
            Id: n.Id,
            RecipientUserId: n.RecipientUserId,
            CaseId: n.CaseId,
            Type: n.Type.ToString(),
            Channel: n.Channel.ToString(),
            Subject: n.Subject,
            Body: n.Body,
            IsRead: n.IsRead,
            ReadAt: n.ReadAt,
            SentAt: n.SentAt,
            CreatedAt: n.CreatedAt);
}
