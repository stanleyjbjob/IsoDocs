using IsoDocs.Application.Customers;
using IsoDocs.Domain.Customers;

namespace IsoDocs.Application.UnitTests.Fakes;

public sealed class FakeCustomerRepository : ICustomerRepository
{
    private readonly Dictionary<Guid, Customer> _store = new();

    public IReadOnlyDictionary<Guid, Customer> Store => _store;

    public Customer Seed(string code, string name, bool isActive = true)
    {
        var c = new Customer(Guid.NewGuid(), code, name);
        if (!isActive) c.Deactivate();
        _store[c.Id] = c;
        return c;
    }

    public Task<IReadOnlyList<Customer>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        var result = includeInactive
            ? _store.Values.ToList()
            : _store.Values.Where(c => c.IsActive).ToList();
        return Task.FromResult<IReadOnlyList<Customer>>(result.OrderBy(c => c.Code).ToList());
    }

    public Task<Customer?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id, out var c) ? c : null);

    public Task<Customer?> FindByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Values.FirstOrDefault(c => c.Code == code));

    public Task<Customer?> FindByCodeExcludingIdAsync(string code, Guid excludingId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Values.FirstOrDefault(c => c.Code == code && c.Id != excludingId));

    public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _store[customer.Id] = customer;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
