using IsoDocs.Application.Common.Messaging;

namespace IsoDocs.Application.Cases.Queries;

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
        var relations = await _caseRelations.ListByCaseIdAsync(request.CaseId, cancellationToken);
        return relations.Select(CaseRelationDtoMapper.ToDto).ToList();
    }
}
