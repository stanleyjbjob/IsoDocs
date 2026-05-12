using IsoDocs.Application.Common.Messaging;

namespace IsoDocs.Application.Cases.Queries;

/// <summary>
/// GET /api/cases/search — 全文關鍵字搜尋。
/// </summary>
public sealed record SearchCasesQuery(string Keyword, ListCasesFilter Filter) : IQuery<PagedResult<CaseSummaryDto>>;

public sealed class SearchCasesQueryHandler : IQueryHandler<SearchCasesQuery, PagedResult<CaseSummaryDto>>
{
    private readonly ICaseQueryService _queryService;

    public SearchCasesQueryHandler(ICaseQueryService queryService)
    {
        _queryService = queryService;
    }

    public Task<PagedResult<CaseSummaryDto>> Handle(SearchCasesQuery request, CancellationToken cancellationToken)
        => _queryService.SearchAsync(request.Keyword, request.Filter, cancellationToken);
}
