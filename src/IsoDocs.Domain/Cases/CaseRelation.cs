using IsoDocs.Domain.Common;

namespace IsoDocs.Domain.Cases;

/// <summary>
/// 案件之間的關聯。可表示主子流程、重開新案、參考關聯等。
/// </summary>
public class CaseRelation : Entity<Guid>
{
    public Guid ParentCaseId { get; protected set; }
    public Guid ChildCaseId { get; protected set; }
    public CaseRelationType RelationType { get; protected set; }
    public Guid CreatedByUserId { get; protected set; }

    private CaseRelation() { }

    public CaseRelation(Guid id, Guid parentCaseId, Guid childCaseId, CaseRelationType relationType, Guid createdByUserId)
    {
        if (parentCaseId == childCaseId)
            throw new DomainException("case_relation.self_relation", "不可建立自我關聯");

        Id = id;
        ParentCaseId = parentCaseId;
        ChildCaseId = childCaseId;
        RelationType = relationType;
        CreatedByUserId = createdByUserId;
    }
}
