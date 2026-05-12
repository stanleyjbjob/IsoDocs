using IsoDocs.Application.Common.Messaging;

namespace IsoDocs.Application.Cases.Queries;

public sealed record GetMyInitiatedCasesQuery(Guid UserId) : IQuery<IReadOnlyList<CaseSummaryDto>>;

public sealed class GetMyInitiatedCasesQueryHandler : IQueryHandler<GetMyInitiatedCasesQuery, IReadOnlyList<CaseSummaryDto>>
{
    private readonly ICaseDashboardService _dashboard;

    public GetMyInitiatedCasesQueryHandler(ICaseDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    public Task<IReadOnlyList<CaseSummaryDto>> Handle(GetMyInitiatedCasesQuery request, CancellationToken cancellationToken)
        => _dashboard.GetMyInitiatedCasesAsync(request.UserId, cancellationToken);
}
