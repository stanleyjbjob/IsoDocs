using IsoDocs.Domain.Customers;

namespace IsoDocs.Application.Customers;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default);
    Task<Customer?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Customer?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Customer?> FindByCodeExcludingIdAsync(string code, Guid excludingId, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
