using IsoDocs.Application.Cases.Comments;
using IsoDocs.Domain.Communications;

namespace IsoDocs.Application.UnitTests.Fakes;

/// <summary>
/// <see cref="ICommentRepository"/> 的記憶體 fake 實作。不接 DB、不需 EF Core。
/// SaveChangesAsync 是 no-op。測試可透過 <see cref="SeedCase"/> 和 <see cref="Seed"/> 預先資料。
/// </summary>
public sealed class FakeCommentRepository : ICommentRepository
{
    private readonly Dictionary<Guid, Comment> _store = new();
    private readonly HashSet<Guid> _caseIds = new();

    /// <summary>註冊一個存在的案件 ID，讓 CaseExistsAsync 回傳 true。</summary>
    public void SeedCase(Guid caseId) => _caseIds.Add(caseId);

    /// <summary>預先新增一則留言至儲存區。</summary>
    public Comment Seed(Guid caseId, Guid authorUserId, string body)
    {
        var comment = new Comment(Guid.NewGuid(), caseId, authorUserId, body, null);
        _store[comment.Id] = comment;
        return comment;
    }

    public Task<bool> CaseExistsAsync(Guid caseId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_caseIds.Contains(caseId));

    public Task<IReadOnlyList<Comment>> ListByCaseIdAsync(
        Guid caseId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Comment>>(
            _store.Values
                .Where(c => c.CaseId == caseId && !c.IsDeleted)
                .OrderBy(c => c.CreatedAt)
                .ThenBy(c => c.Id)
                .ToList());

    public Task AddAsync(Comment comment, CancellationToken cancellationToken = default)
    {
        _store[comment.Id] = comment;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
