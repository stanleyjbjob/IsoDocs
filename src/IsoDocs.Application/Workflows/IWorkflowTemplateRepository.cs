using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.Workflows;

/// <summary>
/// 流程範本資料存取抽象。Infrastructure 層以 EF Core 實作；測試以 in-memory fake 實作。
/// </summary>
public interface IWorkflowTemplateRepository
{
    Task<IReadOnlyList<WorkflowTemplate>> ListAsync(bool includeInactive = true, CancellationToken cancellationToken = default);
    Task<WorkflowTemplate?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkflowTemplate?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<WorkflowTemplate?> FindByCodeExcludingIdAsync(string code, Guid excludingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowNode>> ListNodesAsync(Guid templateId, int version, CancellationToken cancellationToken = default);
    Task AddAsync(WorkflowTemplate template, CancellationToken cancellationToken = default);
    Task AddNodesAsync(IEnumerable<WorkflowNode> nodes, CancellationToken cancellationToken = default);
    Task RemoveNodesAsync(Guid templateId, int version, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
