using IsoDocs.Application.Cases;
using IsoDocs.Domain.Workflows;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Persistence.Repositories;

/// <summary>
/// <see cref="IDocumentTypeRepository"/> 的 EF Core 實作。
/// FindByIdAsync 使用追蹤查詢，讓 AcquireNext 的 CurrentSequence 異動可被 EF 偵測。
/// </summary>
internal sealed class DocumentTypeRepository : IDocumentTypeRepository
{
    private readonly IsoDocsDbContext _dbContext;

    public DocumentTypeRepository(IsoDocsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DocumentType?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.DocumentTypes.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
