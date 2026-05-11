using IsoDocs.Application.Cases;
using IsoDocs.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core 實作的案件 Repository。
/// </summary>
public sealed class CaseRepository : ICaseRepository
{
    private readonly IsoDocsDbContext _context;

    public CaseRepository(IsoDocsDbContext context)
    {
        _context = context;
    }

    public async Task<Case?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Cases.FindAsync(new object[] { id }, cancellationToken);

    public async Task<IReadOnlyList<CaseNode>> GetNodesAsync(Guid caseId, CancellationToken cancellationToken = default)
        => await _context.CaseNodes
            .Where(n => n.CaseId == caseId)
            .OrderBy(n => n.NodeOrder)
            .ToListAsync(cancellationToken);

    public async Task AddActionAsync(CaseAction action, CancellationToken cancellationToken = default)
        => await _context.CaseActions.AddAsync(action, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
