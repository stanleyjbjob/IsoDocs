using IsoDocs.Application.Cases.Export;
using IsoDocs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Pdf;

internal sealed class CasePdfDataProvider : ICasePdfDataProvider
{
    private readonly IsoDocsDbContext _db;

    public CasePdfDataProvider(IsoDocsDbContext db) => _db = db;

    public async Task<CasePdfData?> GetAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var c = await _db.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == caseId, cancellationToken);
        if (c is null)
            return null;

        var userIds = new HashSet<Guid> { c.InitiatedByUserId };

        var nodes = await _db.CaseNodes
            .AsNoTracking()
            .Where(x => x.CaseId == caseId)
            .OrderBy(x => x.NodeOrder)
            .ToListAsync(cancellationToken);
        foreach (var n in nodes)
            if (n.AssigneeUserId.HasValue)
                userIds.Add(n.AssigneeUserId.Value);

        var actions = await _db.CaseActions
            .AsNoTracking()
            .Where(x => x.CaseId == caseId)
            .OrderBy(x => x.ActionAt)
            .ToListAsync(cancellationToken);
        foreach (var a in actions)
            userIds.Add(a.ActorUserId);

        var comments = await _db.Comments
            .AsNoTracking()
            .Where(x => x.CaseId == caseId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        foreach (var cm in comments)
            userIds.Add(cm.AuthorUserId);

        var attachments = await _db.Attachments
            .AsNoTracking()
            .Where(x => x.CaseId == caseId && !x.IsDeleted)
            .OrderBy(x => x.UploadedAt)
            .ToListAsync(cancellationToken);
        foreach (var at in attachments)
            userIds.Add(at.UploadedByUserId);

        var fields = await _db.CaseFields
            .AsNoTracking()
            .Where(x => x.CaseId == caseId)
            .OrderBy(x => x.FieldCode)
            .ToListAsync(cancellationToken);

        var users = await _db.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .Select(x => new { x.Id, x.DisplayName })
            .ToDictionaryAsync(x => x.Id, x => x.DisplayName, cancellationToken);

        string UserName(Guid id) =>
            users.TryGetValue(id, out var name) ? name : id.ToString()[..8];

        string? customerName = null;
        if (c.CustomerId.HasValue)
        {
            customerName = await _db.Customers
                .AsNoTracking()
                .Where(x => x.Id == c.CustomerId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new CasePdfData(
            CaseNumber: c.CaseNumber,
            Title: c.Title,
            Status: c.Status.ToString(),
            InitiatedByUserName: UserName(c.InitiatedByUserId),
            InitiatedAt: c.InitiatedAt,
            ExpectedCompletionAt: c.ExpectedCompletionAt,
            OriginalExpectedAt: c.OriginalExpectedAt,
            ClosedAt: c.ClosedAt,
            VoidedAt: c.VoidedAt,
            CustomVersionNumber: c.CustomVersionNumber,
            CustomerName: customerName,
            Fields: fields
                .Select(f => new CaseFieldPdfItem(f.FieldCode, f.ValueJson))
                .ToList(),
            Nodes: nodes
                .Select(n => new CaseNodePdfItem(
                    n.NodeOrder, n.NodeName, n.Status.ToString(),
                    n.AssigneeUserId.HasValue ? UserName(n.AssigneeUserId.Value) : null,
                    n.StartedAt, n.CompletedAt, n.ModifiedExpectedAt))
                .ToList(),
            Actions: actions
                .Select(a => new CaseActionPdfItem(
                    a.ActionType.ToString(), UserName(a.ActorUserId), a.Comment, a.ActionAt))
                .ToList(),
            Comments: comments
                .Select(cm => new CommentPdfItem(
                    UserName(cm.AuthorUserId), cm.Body, cm.CreatedAt))
                .ToList(),
            Attachments: attachments
                .Select(at => new AttachmentPdfItem(
                    at.FileName, at.ContentType, at.SizeBytes, at.UploadedAt,
                    UserName(at.UploadedByUserId)))
                .ToList());
    }
}
