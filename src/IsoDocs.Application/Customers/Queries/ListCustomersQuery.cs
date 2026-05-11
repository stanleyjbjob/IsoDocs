using IsoDocs.Application.Common.Messaging;

namespace IsoDocs.Application.Customers.Queries;

public sealed record ListCustomersQuery(bool IncludeInactive = true) : IQuery<IReadOnlyList<CustomerDto>>;

public sealed class ListCustomersQueryHandler : IQueryHandler<ListCustomersQuery, IReadOnlyList<CustomerDto>>
{
    private readonly ICustomerRepository _customers;

    public ListCustomersQueryHandler(ICustomerRepository customers)
    {
        _customers = customers;
    }

    public async Task<IReadOnlyList<CustomerDto>> Handle(ListCustomersQuery request, CancellationToken cancellationToken)
    {
        var list = await _customers.ListAsync(request.IncludeInactive, cancellationToken);
        return list.Select(CustomerDtoMapper.ToDto).ToList();
    }
}
