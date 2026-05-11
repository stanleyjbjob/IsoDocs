using IsoDocs.Application.DocumentTypes;
using IsoDocs.Domain.Common;
using IsoDocs.Domain.Workflows;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Persistence.Repositories;

internal sealed class DocumentTypeRepository : IDocumentTypeRepository
{
    private const int MaxConcurrencyRetries = 5;
    private readonly IsoDocsDbContext _db;

    public DocumentTypeRepository(IsoDocsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<DocumentType>> ListAsync(
        bool includeInactive, CancellationToken cancellationToken = default)
    {
        var q = _db.DocumentTypes.AsNoTracking();
        if (!includeInactive)
            q = q.Where(d => d.IsActive);
        return await q.OrderBy(d => d.CompanyCode).ThenBy(d => d.Code).ToListAsync(cancellationToken);
    }

    public Task<DocumentType?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.DocumentTypes.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<DocumentType?> FindByCompanyAndCodeAsync(
        string companyCode, string code, CancellationToken cancellationToken = default)
        => _db.DocumentTypes.AsNoTracking()
            .FirstOrDefaultAsync(d => d.CompanyCode == companyCode && d.Code == code, cancellationToken);

    public async Task AddAsync(DocumentType documentType, CancellationToken cancellationToken = default)
        => await _db.DocumentTypes.AddAsync(documentType, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);

    /// <summary>
    /// 帶樂觀鎖重試的取號。衝突時將實體從 Change Tracker 卸離後重新讀取，
    /// 確保每次嘗試都取到最新 RowVersion。
    /// </summary>
    public async Task<string> AcquireNextCodeAsync(
        Guid documentTypeId, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt <= MaxConcurrencyRetries; attempt++)
        {
            var dt = await _db.DocumentTypes
                .FirstOrDefaultAsync(d => d.Id == documentTypeId, cancellationToken)
                ?? throw new DomainException("DOCUMENT_TYPE_NOT_FOUND",
                    $"DocumentType '{documentTypeId}' 不存在。");

            if (!dt.IsActive)
                throw new DomainException("DOCUMENT_TYPE_NOT_FOUND",
                    $"DocumentType '{documentTypeId}' 已停用，無法取號。");

            var code = dt.AcquireNext(DateTimeOffset.UtcNow.Year);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return code;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxConcurrencyRetries)
            {
                // 卸離舊追蹤，下次迴圈重新從 DB 讀取最新 RowVersion
                _db.Entry(dt).State = EntityState.Detached;
            }
        }

        throw new DomainException("DOCUMENT_TYPE_CONCURRENCY_CONFLICT",
            "取號時發生持續的併發衝突，請稍後重試。");
    }
}
