using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;

namespace IsoDocs.Application.Customers.Commands;

public sealed record UpdateCustomerCommand(
    Guid CustomerId,
    string Name,
    string? ContactPerson,
    string? ContactEmail,
    string? ContactPhone,
    string? Note) : ICommand<CustomerDto>;

public sealed class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("客戶名稱必填")
            .MaximumLength(256).WithMessage("客戶名稱不可超過 256 字");

        RuleFor(x => x.ContactEmail)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail))
            .WithMessage("聯絡 Email 格式不正確");
    }
}

public sealed class UpdateCustomerCommandHandler : ICommandHandler<UpdateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _customers;

    public UpdateCustomerCommandHandler(ICustomerRepository customers)
    {
        _customers = customers;
    }

    public async Task<CustomerDto> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customers.FindByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null)
            throw new DomainException(CustomerErrorCodes.NotFound, $"找不到客戶 Id={request.CustomerId}。");

        customer.Update(request.Name, request.ContactPerson, request.ContactEmail, request.ContactPhone, request.Note);
        await _customers.SaveChangesAsync(cancellationToken);

        return CustomerDtoMapper.ToDto(customer);
    }
}
