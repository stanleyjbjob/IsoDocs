using IsoDocs.Application.Common.Messaging;

namespace IsoDocs.Application.Cases.Queries;

/// <summary>
/// GET /api/cases — 多條件篩選與排序。
/// </summary>
public sealed record ListCasesQuery(ListCasesFilter Filter) : IQuery<PagedResult<CaseSummaryDto>>;

public sealed class ListCasesQueryHandler : IQueryHandler<ListCasesQuery, PagedResult<CaseSummaryDto>>
{
    private readonly ICaseQueryService _queryService;

    public ListCasesQueryHandler(ICaseQueryService queryService)
    {
        _queryService = queryService;
    }

    public Task<PagedResult<CaseSummaryDto>> Handle(ListCasesQuery request, CancellationToken cancellationToken)
        => _queryService.ListAsync(request.Filter, cancellationToken);
}
