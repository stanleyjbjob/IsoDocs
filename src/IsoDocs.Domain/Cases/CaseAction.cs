using IsoDocs.Domain.Common;

namespace IsoDocs.Domain.Cases;

/// <summary>
/// 案件動作軌跡。任何變動都寫入一條，提供簽核軌跡、審查與年度稽核。
/// </summary>
public class CaseAction : Entity<Guid>
{
    public Guid CaseId { get; protected set; }
    public Guid? CaseNodeId { get; protected set; }
    public CaseActionType ActionType { get; protected set; }
    public Guid ActorUserId { get; protected set; }
    public string? Comment { get; protected set; }
    /// <summary>動作發生時間。對應需求中的 ActionAt。</summary>
    public DateTimeOffset ActionAt { get; protected set; } = DateTimeOffset.UtcNow;
    /// <summary>動作 payload（例如退回原因、變更前後值等），以 JSON 儲存。</summary>
    public string? PayloadJson { get; protected set; }

    private CaseAction() { }

    public CaseAction(Guid id, Guid caseId, Guid? caseNodeId, CaseActionType actionType, Guid actorUserId, string? comment, string? payloadJson)
    {
        Id = id;
        CaseId = caseId;
        CaseNodeId = caseNodeId;
        ActionType = actionType;
        ActorUserId = actorUserId;
        Comment = comment;
        PayloadJson = payloadJson;
    }
}
