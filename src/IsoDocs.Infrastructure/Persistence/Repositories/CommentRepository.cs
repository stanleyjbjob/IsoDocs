using IsoDocs.Application.Cases.Comments;
using IsoDocs.Domain.Communications;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Persistence.Repositories;

/// <summary>
/// <see cref="ICommentRepository"/> 的 EF Core 實作。Scoped 同 DbContext。
/// </summary>
internal sealed class CommentRepository : ICommentRepository
{
    private readonly IsoDocsDbContext _dbContext;

    public CommentRepository(IsoDocsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> CaseExistsAsync(Guid caseId, CancellationToken cancellationToken = default) =>
        _dbContext.Cases.AnyAsync(c => c.Id == caseId, cancellationToken);

    public async Task<IReadOnlyList<Comment>> ListByCaseIdAsync(
        Guid caseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Comments
            .AsNoTracking()
            .Where(c => c.CaseId == caseId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Comments.AddAsync(comment, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
