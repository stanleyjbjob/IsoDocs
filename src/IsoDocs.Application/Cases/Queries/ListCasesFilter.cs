using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.Cases.Queries;

/// <summary>
/// GET /api/cases 多條件篩選參數。
/// </summary>
public sealed record ListCasesFilter(
    CaseStatus? Status = null,
    Guid? DocumentTypeId = null,
    DateTimeOffset? InitiatedFrom = null,
    DateTimeOffset? InitiatedTo = null,
    Guid? CustomerId = null,
    string? CaseNumberPrefix = null,
    string? SortBy = null,
    bool SortDescending = false,
    int Page = 1,
    int PageSize = 20);
