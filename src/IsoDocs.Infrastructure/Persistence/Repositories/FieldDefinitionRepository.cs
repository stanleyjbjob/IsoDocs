using IsoDocs.Application.FieldDefinitions;
using IsoDocs.Domain.Workflows;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Persistence.Repositories;

internal sealed class FieldDefinitionRepository : IFieldDefinitionRepository
{
    private readonly IsoDocsDbContext _dbContext;

    public FieldDefinitionRepository(IsoDocsDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<FieldDefinition>> ListAsync(
        bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.FieldDefinitions.AsNoTracking();
        if (!includeInactive)
            query = query.Where(f => f.IsActive);
        return await query.OrderBy(f => f.Code).ToListAsync(cancellationToken);
    }

    public Task<FieldDefinition?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.FieldDefinitions.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public Task<FieldDefinition?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
        => _dbContext.FieldDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Code == code, cancellationToken);

    public async Task AddAsync(FieldDefinition fieldDefinition, CancellationToken cancellationToken = default)
        => await _dbContext.FieldDefinitions.AddAsync(fieldDefinition, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
