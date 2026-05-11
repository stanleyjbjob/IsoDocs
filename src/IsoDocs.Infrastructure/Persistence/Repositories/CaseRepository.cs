using IsoDocs.Application.Cases;
using IsoDocs.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Persistence.Repositories;

internal sealed class CaseRepository : ICaseRepository
{
    private readonly IsoDocsDbContext _dbContext;

    public CaseRepository(IsoDocsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Case?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Cases.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(Case caseEntity, CancellationToken cancellationToken = default)
        => await _dbContext.Cases.AddAsync(caseEntity, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
