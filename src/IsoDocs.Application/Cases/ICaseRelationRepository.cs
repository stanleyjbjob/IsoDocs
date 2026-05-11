using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.Cases;

public interface ICaseRelationRepository
{
    Task<IReadOnlyList<CaseRelation>> GetRelationsByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default);

    /// <summary>主單結案前使用：確認是否有尚未完成的子流程。</summary>
    Task<bool> HasIncompleteSubprocessesAsync(Guid parentCaseId, CancellationToken cancellationToken = default);

    Task AddAsync(CaseRelation relation, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
