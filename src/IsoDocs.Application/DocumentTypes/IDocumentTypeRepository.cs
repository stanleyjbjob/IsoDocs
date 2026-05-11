using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.DocumentTypes;

public interface IDocumentTypeRepository
{
    Task<IReadOnlyList<DocumentType>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default);
    Task<DocumentType?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentType?> FindByCompanyAndCodeAsync(string companyCode, string code, CancellationToken cancellationToken = default);
    Task AddAsync(DocumentType documentType, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 帶樂觀鎖重試的取號，最多重試 5 次。
    /// 併發衝突超過上限時拋 <see cref="Domain.Common.DomainException"/>。
    /// </summary>
    Task<string> AcquireNextCodeAsync(Guid documentTypeId, CancellationToken cancellationToken = default);
}
