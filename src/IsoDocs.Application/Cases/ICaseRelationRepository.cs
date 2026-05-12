using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.Cases;

public interface ICaseRelationRepository
{
    Task<IReadOnlyList<CaseRelation>> ListByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task AddAsync(CaseRelation relation, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
