using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.FieldDefinitions;

/// <summary>
/// 欄位定義資料存取抽象。Infrastructure 層以 EF Core 實作；測試以 in-memory fake 實作。
/// </summary>
public interface IFieldDefinitionRepository
{
    Task<IReadOnlyList<FieldDefinition>> ListAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<FieldDefinition?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FieldDefinition?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(FieldDefinition fieldDefinition, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
