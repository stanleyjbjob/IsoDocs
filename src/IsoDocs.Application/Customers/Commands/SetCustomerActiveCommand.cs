using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;
using MediatR;

namespace IsoDocs.Application.Customers.Commands;

public sealed record SetCustomerActiveCommand(Guid CustomerId, bool IsActive) : ICommand;

public sealed class SetCustomerActiveCommandHandler : ICommandHandler<SetCustomerActiveCommand>
{
    private readonly ICustomerRepository _customers;

    public SetCustomerActiveCommandHandler(ICustomerRepository customers)
    {
        _customers = customers;
    }

    public async Task<Unit> Handle(SetCustomerActiveCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customers.FindByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
            throw new DomainException(CustomerErrorCodes.NotFound, $"找不到客戶 Id={request.CustomerId}。");

        if (request.IsActive)
            customer.Activate();
        else
            customer.Deactivate();

        await _customers.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
