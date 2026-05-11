using IsoDocs.Application.Workflows;
using IsoDocs.Domain.Workflows;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Persistence.Repositories;

/// <summary>
/// <see cref="IWorkflowTemplateRepository"/> 的 EF Core 實作。Scoped 同 DbContext。
/// </summary>
internal sealed class WorkflowTemplateRepository : IWorkflowTemplateRepository
{
    private readonly IsoDocsDbContext _dbContext;

    public WorkflowTemplateRepository(IsoDocsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<WorkflowTemplate>> ListAsync(bool includeInactive = true, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.WorkflowTemplates.AsNoTracking();
        if (!includeInactive)
            query = query.Where(t => t.IsActive);
        return await query.OrderBy(t => t.Name).ToListAsync(cancellationToken);
    }

    public Task<WorkflowTemplate?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.WorkflowTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<WorkflowTemplate?> FindByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _dbContext.WorkflowTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code == code, cancellationToken);

    public Task<WorkflowTemplate?> FindByCodeExcludingIdAsync(string code, Guid excludingId, CancellationToken cancellationToken = default) =>
        _dbContext.WorkflowTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Code == code && t.Id != excludingId, cancellationToken);

    public async Task<IReadOnlyList<WorkflowNode>> ListNodesAsync(Guid templateId, int version, CancellationToken cancellationToken = default) =>
        await _dbContext.WorkflowNodes
            .AsNoTracking()
            .Where(n => n.WorkflowTemplateId == templateId && n.TemplateVersion == version)
            .OrderBy(n => n.NodeOrder)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(WorkflowTemplate template, CancellationToken cancellationToken = default) =>
        await _dbContext.WorkflowTemplates.AddAsync(template, cancellationToken);

    public async Task AddNodesAsync(IEnumerable<WorkflowNode> nodes, CancellationToken cancellationToken = default) =>
        await _dbContext.WorkflowNodes.AddRangeAsync(nodes, cancellationToken);

    public async Task RemoveNodesAsync(Guid templateId, int version, CancellationToken cancellationToken = default)
    {
        var nodes = await _dbContext.WorkflowNodes
            .Where(n => n.WorkflowTemplateId == templateId && n.TemplateVersion == version)
            .ToListAsync(cancellationToken);
        _dbContext.WorkflowNodes.RemoveRange(nodes);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
