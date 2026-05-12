using IsoDocs.Application.Cases;
using IsoDocs.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Persistence.Repositories;

/// <summary>
/// <see cref="ICaseRepository"/> 的 EF Core 實作。
/// </summary>
public sealed class CaseRepository : ICaseRepository
{
    private readonly IsoDocsDbContext _db;

    public CaseRepository(IsoDocsDbContext db)
    {
        _db = db;
    }

    public Task<Case?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Cases.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<CaseNode?> FindNodeByIdAsync(Guid nodeId, CancellationToken cancellationToken = default) =>
        _db.CaseNodes.FirstOrDefaultAsync(n => n.Id == nodeId, cancellationToken);

    public async Task<IReadOnlyList<SignOffEntryDto>> GetSignOffTrailAsync(
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        var result = await (
            from a in _db.CaseActions
            join n in _db.CaseNodes on a.CaseNodeId equals n.Id into nodeJoin
            from n in nodeJoin.DefaultIfEmpty()
            where a.CaseId == caseId && a.ActionType == CaseActionType.SignOff
            orderby a.ActionAt
            select new SignOffEntryDto(
                a.Id,
                a.CaseId,
                a.CaseNodeId,
                n != null ? n.NodeName : null,
                a.ActorUserId,
                a.Comment,
                a.ActionAt)
        ).ToListAsync(cancellationToken);

        return result;
    }

    public async Task AddActionAsync(CaseAction action, CancellationToken cancellationToken = default)
    {
        await _db.CaseActions.AddAsync(action, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
