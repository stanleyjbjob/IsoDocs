using IsoDocs.Domain.Common;

namespace IsoDocs.Domain.Audit;

/// <summary>
/// 系統層面的稽核軌跡（不同於 CaseAction 的業務軌跡）。
/// 記錄 entity 變動、權限變更、登入登出等系統事件。
/// </summary>
public class AuditTrail : Entity<Guid>
{
    public Guid? UserId { get; protected set; }
    public string EntityType { get; protected set; } = string.Empty;
    public string EntityId { get; protected set; } = string.Empty;
    public string Action { get; protected set; } = string.Empty;
    public string? ChangesJson { get; protected set; }
    public string? IpAddress { get; protected set; }
    public string? UserAgent { get; protected set; }
    public DateTimeOffset OccurredAt { get; protected set; } = DateTimeOffset.UtcNow;

    private AuditTrail() { }

    public AuditTrail(Guid id, Guid? userId, string entityType, string entityId, string action, string? changesJson, string? ipAddress, string? userAgent)
    {
        Id = id;
        UserId = userId;
        EntityType = entityType;
        EntityId = entityId;
        Action = action;
        ChangesJson = changesJson;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }
}
