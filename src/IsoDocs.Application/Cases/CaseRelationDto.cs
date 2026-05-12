using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.Cases;

public sealed record CaseRelationDto(
    Guid Id,
    Guid ParentCaseId,
    Guid ChildCaseId,
    string RelationType,
    Guid CreatedByUserId);

public static class CaseRelationDtoMapper
{
    public static CaseRelationDto ToDto(CaseRelation r) => new(
        Id: r.Id,
        ParentCaseId: r.ParentCaseId,
        ChildCaseId: r.ChildCaseId,
        RelationType: r.RelationType.ToString(),
        CreatedByUserId: r.CreatedByUserId);
}
