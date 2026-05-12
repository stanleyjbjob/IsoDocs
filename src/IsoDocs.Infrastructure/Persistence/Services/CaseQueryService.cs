using IsoDocs.Application.Cases.Queries;
using IsoDocs.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Persistence.Services;

/// <summary>
/// <see cref="ICaseQueryService"/> 的 EF Core 實作。
/// 搜尋以 LIKE 模糊比對；生產環境建議改用 SQL Server Full-Text Search 或 Azure AI Search。
/// </summary>
internal sealed class CaseQueryService : ICaseQueryService
{
    private readonly IsoDocsDbContext _db;

    public CaseQueryService(IsoDocsDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<CaseSummaryDto>> ListAsync(
        ListCasesFilter filter, CancellationToken cancellationToken = default)
    {
        var caseQuery = BuildBaseQuery(filter);
        var projected = ProjectToDto(caseQuery);
        projected = ApplySort(projected, filter);
        return await PageAsync(projected, filter.Page, filter.PageSize, cancellationToken);
    }

    public async Task<PagedResult<CaseSummaryDto>> SearchAsync(
        string keyword, ListCasesFilter filter, CancellationToken cancellationToken = default)
    {
        var caseQuery = BuildBaseQuery(filter);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword}%";
            caseQuery = caseQuery.Where(c =>
                EF.Functions.Like(c.CaseNumber, pattern) ||
                EF.Functions.Like(c.Title, pattern) ||
                (c.CustomVersionNumber != null && EF.Functions.Like(c.CustomVersionNumber, pattern)));
        }
        var projected = ProjectToDto(caseQuery);
        projected = ApplySort(projected, filter);
        return await PageAsync(projected, filter.Page, filter.PageSize, cancellationToken);
    }

    private IQueryable<Case> BuildBaseQuery(ListCasesFilter f)
    {
        var q = _db.Cases.AsNoTracking();
        if (f.Status.HasValue)
            q = q.Where(c => c.Status == f.Status.Value);
        if (f.DocumentTypeId.HasValue)
            q = q.Where(c => c.DocumentTypeId == f.DocumentTypeId.Value);
        if (f.CustomerId.HasValue)
            q = q.Where(c => c.CustomerId == f.CustomerId.Value);
        if (f.InitiatedFrom.HasValue)
            q = q.Where(c => c.InitiatedAt >= f.InitiatedFrom.Value);
        if (f.InitiatedTo.HasValue)
            q = q.Where(c => c.InitiatedAt <= f.InitiatedTo.Value);
        if (!string.IsNullOrWhiteSpace(f.CaseNumberPrefix))
            q = q.Where(c => c.CaseNumber.StartsWith(f.CaseNumberPrefix));
        return q;
    }

    private IQueryable<CaseSummaryDto> ProjectToDto(IQueryable<Case> caseQuery)
    {
        return from c in caseQuery
               join dt in _db.DocumentTypes on c.DocumentTypeId equals dt.Id
               join cu in _db.Customers on c.CustomerId equals (Guid?)cu.Id into customers
               from customer in customers.DefaultIfEmpty()
               select new CaseSummaryDto(
                   c.Id,
                   c.CaseNumber,
                   c.Title,
                   c.Status,
                   c.DocumentTypeId,
                   dt.Name,
                   c.CustomerId,
                   customer != null ? customer.Name : null,
                   c.InitiatedAt,
                   c.ExpectedCompletionAt,
                   c.ClosedAt,
                   c.VoidedAt,
                   c.CustomVersionNumber);
    }

    private static IQueryable<CaseSummaryDto> ApplySort(IQueryable<CaseSummaryDto> q, ListCasesFilter f)
    {
        return (f.SortBy?.ToLowerInvariant(), f.SortDescending) switch
        {
            ("casenumber", false) => q.OrderBy(c => c.CaseNumber),
            ("casenumber", true) => q.OrderByDescending(c => c.CaseNumber),
            ("title", false) => q.OrderBy(c => c.Title),
            ("title", true) => q.OrderByDescending(c => c.Title),
            ("status", false) => q.OrderBy(c => c.Status),
            ("status", true) => q.OrderByDescending(c => c.Status),
            ("expectedcompletionat", false) => q.OrderBy(c => c.ExpectedCompletionAt),
            ("expectedcompletionat", true) => q.OrderByDescending(c => c.ExpectedCompletionAt),
            _ => q.OrderByDescending(c => c.InitiatedAt)
        };
    }

    private static async Task<PagedResult<CaseSummaryDto>> PageAsync(
        IQueryable<CaseSummaryDto> query, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return new PagedResult<CaseSummaryDto>(items, totalCount, page, pageSize);
    }
}
