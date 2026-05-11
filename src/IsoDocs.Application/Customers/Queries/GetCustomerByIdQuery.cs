using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;

namespace IsoDocs.Application.Customers.Queries;

public sealed record GetCustomerByIdQuery(Guid CustomerId) : IQuery<CustomerDto>;

public sealed class GetCustomerByIdQueryHandler : IQueryHandler<GetCustomerByIdQuery, CustomerDto>
{
    private readonly ICustomerRepository _customers;

    public GetCustomerByIdQueryHandler(ICustomerRepository customers)
    {
        _customers = customers;
    }

    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customers.FindByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
            throw new DomainException(CustomerErrorCodes.NotFound, $"找不到客戶 Id={request.CustomerId}。");

        return CustomerDtoMapper.ToDto(customer);
    }
}
