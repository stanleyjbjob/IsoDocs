using IsoDocs.Application.Common.Messaging;

namespace IsoDocs.Application.Cases.Queries;

public sealed record GetAllCasesQuery(
    string? StatusFilter = null,
    int Page = 1,
    int PageSize = 50) : IQuery<IReadOnlyList<CaseSummaryDto>>;

public sealed class GetAllCasesQueryHandler : IQueryHandler<GetAllCasesQuery, IReadOnlyList<CaseSummaryDto>>
{
    private readonly ICaseDashboardService _dashboard;

    public GetAllCasesQueryHandler(ICaseDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    public Task<IReadOnlyList<CaseSummaryDto>> Handle(GetAllCasesQuery request, CancellationToken cancellationToken)
        => _dashboard.GetAllCasesAsync(request.StatusFilter, request.Page, request.PageSize, cancellationToken);
}
