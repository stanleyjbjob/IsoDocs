using IsoDocs.Application.Customers;
using IsoDocs.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace IsoDocs.Infrastructure.Persistence.Repositories;

internal sealed class CustomerRepository : ICustomerRepository
{
    private readonly IsoDocsDbContext _dbContext;

    public CustomerRepository(IsoDocsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Customer>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Customers.AsNoTracking();
        if (!includeInactive)
            query = query.Where(c => c.IsActive);
        return await query.OrderBy(c => c.Code).ToListAsync(cancellationToken);
    }

    public Task<Customer?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Customer?> FindByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Code == code, cancellationToken);

    public Task<Customer?> FindByCodeExcludingIdAsync(string code, Guid excludingId, CancellationToken cancellationToken = default) =>
        _dbContext.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Code == code && c.Id != excludingId, cancellationToken);

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default) =>
        await _dbContext.Customers.AddAsync(customer, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
