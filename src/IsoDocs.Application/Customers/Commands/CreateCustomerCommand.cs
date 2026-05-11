using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;
using IsoDocs.Domain.Customers;

namespace IsoDocs.Application.Customers.Commands;

public sealed record CreateCustomerCommand(
    string Code,
    string Name,
    string? ContactPerson,
    string? ContactEmail,
    string? ContactPhone,
    string? Note) : ICommand<CustomerDto>;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("客戶代碼必填")
            .MaximumLength(64).WithMessage("客戶代碼不可超過 64 字");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("客戶名稱必填")
            .MaximumLength(256).WithMessage("客戶名稱不可超過 256 字");

        RuleFor(x => x.ContactEmail)
            .EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail))
            .WithMessage("聯絡 Email 格式不正確");
    }
}

public sealed class CreateCustomerCommandHandler : ICommandHandler<CreateCustomerCommand, CustomerDto>
{
    private readonly ICustomerRepository _customers;

    public CreateCustomerCommandHandler(ICustomerRepository customers)
    {
        _customers = customers;
    }

    public async Task<CustomerDto> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var existing = await _customers.FindByCodeAsync(request.Code, cancellationToken);
        if (existing is not null)
            throw new DomainException(CustomerErrorCodes.CodeDuplicate, $"客戶代碼 '{request.Code}' 已存在。");

        var customer = new Customer(Guid.NewGuid(), request.Code, request.Name);
        customer.Update(request.Name, request.ContactPerson, request.ContactEmail, request.ContactPhone, request.Note);

        await _customers.AddAsync(customer, cancellationToken);
        await _customers.SaveChangesAsync(cancellationToken);

        return CustomerDtoMapper.ToDto(customer);
    }
}
