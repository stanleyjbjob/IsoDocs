using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.Cases;

public sealed record CaseRelationDto(
    Guid Id,
    Guid ParentCaseId,
    Guid ChildCaseId,
    CaseRelationType RelationType,
    string RelationTypeLabel,
    Guid CreatedByUserId);
