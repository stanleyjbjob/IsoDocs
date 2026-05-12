using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.Cases.FieldInheritance;

public interface ICaseFieldRepository
{
    Task<IReadOnlyList<CaseField>> ListByCaseIdAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IReadOnlyList<CaseField> fields, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
