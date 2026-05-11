using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.Cases.Queries;

/// <summary>
/// 取得案件的雙向關聯清單（不論該案件為父或子）。issue [5.3.1]。
/// </summary>
public sealed record GetCaseRelationsQuery(Guid CaseId) : IQuery<IReadOnlyList<CaseRelationDto>>;

public sealed class GetCaseRelationsQueryHandler : IQueryHandler<GetCaseRelationsQuery, IReadOnlyList<CaseRelationDto>>
{
    private readonly ICaseRelationRepository _caseRelations;

    public GetCaseRelationsQueryHandler(ICaseRelationRepository caseRelations)
    {
        _caseRelations = caseRelations;
    }

    public async Task<IReadOnlyList<CaseRelationDto>> Handle(GetCaseRelationsQuery request, CancellationToken cancellationToken)
    {
        var relations = await _caseRelations.GetRelationsByCaseIdAsync(request.CaseId, cancellationToken);

        return relations.Select(r => new CaseRelationDto(
            r.Id,
            r.ParentCaseId,
            r.ChildCaseId,
            r.RelationType,
            r.RelationType switch
            {
                CaseRelationType.Subprocess => "子流程",
                CaseRelationType.Reopen    => "重開新案",
                CaseRelationType.Reference => "參考關聯",
                _ => r.RelationType.ToString()
            },
            r.CreatedByUserId)).ToList();
    }
}
