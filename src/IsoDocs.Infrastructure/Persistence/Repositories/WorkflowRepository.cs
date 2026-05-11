using IsoDocs.Application.Cases;
using IsoDocs.Domain.Workflows;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Persistence.Repositories;

/// <summary>
/// <see cref="IWorkflowRepository"/> 的 EF Core 實作（唯讀）。
/// </summary>
internal sealed class WorkflowRepository : IWorkflowRepository
{
    private readonly IsoDocsDbContext _dbContext;

    public WorkflowRepository(IsoDocsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<WorkflowTemplate?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.WorkflowTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WorkflowNode>> ListNodesByTemplateAsync(
        Guid templateId, int templateVersion, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkflowNodes
            .AsNoTracking()
            .Where(n => n.WorkflowTemplateId == templateId && n.TemplateVersion == templateVersion)
            .OrderBy(n => n.NodeOrder)
            .ToListAsync(cancellationToken);
    }
}
