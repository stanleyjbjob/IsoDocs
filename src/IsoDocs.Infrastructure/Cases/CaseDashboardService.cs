using IsoDocs.Application.Cases;
using IsoDocs.Domain.Cases;
using IsoDocs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Cases;

public sealed class CaseDashboardService : ICaseDashboardService
{
    private readonly IsoDocsDbContext _db;

    public CaseDashboardService(IsoDocsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TodoItemDto>> GetMyTodosAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        var activeStatuses = new[] { CaseNodeStatus.Pending, CaseNodeStatus.InProgress };

        var nodes = await _db.CaseNodes
            .Where(n => n.AssigneeUserId == userId && activeStatuses.Contains(n.Status))
            .ToListAsync(cancellationToken);

        if (nodes.Count == 0)
            return Array.Empty<TodoItemDto>();

        var caseIds = nodes.Select(n => n.CaseId).Distinct().ToList();
        var cases = await _db.Cases
            .Where(c => caseIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        return nodes
            .Where(n => cases.ContainsKey(n.CaseId))
            .OrderBy(n => n.ModifiedExpectedAt ?? n.OriginalExpectedAt)
            .ThenBy(n => n.NodeOrder)
            .Select(n =>
            {
                var c = cases[n.CaseId];
                return new TodoItemDto(
                    CaseNodeId: n.Id,
                    CaseId: c.Id,
                    CaseNumber: c.CaseNumber,
                    CaseTitle: c.Title,
                    NodeName: n.NodeName,
                    NodeOrder: n.NodeOrder,
                    Status: n.Status.ToString(),
                    ExpectedAt: n.ModifiedExpectedAt ?? n.OriginalExpectedAt);
            })
            .ToList();
    }

    public async Task<IReadOnlyList<CaseSummaryDto>> GetMyInitiatedCasesAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        var rawCases = await _db.Cases
            .Where(c => c.InitiatedByUserId == userId)
            .OrderByDescending(c => c.InitiatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return rawCases
            .Select(c => new CaseSummaryDto(
                Id: c.Id,
                CaseNumber: c.CaseNumber,
                Title: c.Title,
                Status: c.Status.ToString(),
                InitiatedAt: c.InitiatedAt,
                ExpectedCompletionAt: c.ExpectedCompletionAt,
                AssigneeDisplayName: null))
            .ToList();
    }

    public async Task<IReadOnlyList<CaseSummaryDto>> GetAllCasesAsync(
        string? statusFilter, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _db.Cases.AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter) &&
            Enum.TryParse<CaseStatus>(statusFilter, out var status))
        {
            query = query.Where(c => c.Status == status);
        }

        var rawCases = await query
            .OrderByDescending(c => c.InitiatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return rawCases
            .Select(c => new CaseSummaryDto(
                Id: c.Id,
                CaseNumber: c.CaseNumber,
                Title: c.Title,
                Status: c.Status.ToString(),
                InitiatedAt: c.InitiatedAt,
                ExpectedCompletionAt: c.ExpectedCompletionAt,
                AssigneeDisplayName: null))
            .ToList();
    }
}
