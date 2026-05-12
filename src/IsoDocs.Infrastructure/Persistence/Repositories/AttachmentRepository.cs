using IsoDocs.Application.Attachments;
using IsoDocs.Domain.Attachments;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Persistence.Repositories;

internal sealed class AttachmentRepository : IAttachmentRepository
{
    private readonly IsoDocsDbContext _db;

    public AttachmentRepository(IsoDocsDbContext db) => _db = db;

    public async Task<IReadOnlyList<Attachment>> ListByCaseIdAsync(Guid caseId, CancellationToken ct = default)
    {
        return await _db.Attachments
            .AsNoTracking()
            .Where(a => a.CaseId == caseId && !a.IsDeleted)
            .OrderBy(a => a.UploadedAt)
            .ToListAsync(ct);
    }

    public Task<Attachment?> FindByIdAsync(Guid id, CancellationToken ct = default)
    {
        return _db.Attachments.FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task AddAsync(Attachment attachment, CancellationToken ct = default)
    {
        await _db.Attachments.AddAsync(attachment, ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return _db.SaveChangesAsync(ct);
    }
}
