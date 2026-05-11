using IsoDocs.Domain.Customers;

namespace IsoDocs.Application.Customers;

public static class CustomerDtoMapper
{
    public static CustomerDto ToDto(Customer c) => new(
        Id: c.Id,
        Code: c.Code,
        Name: c.Name,
        ContactPerson: c.ContactPerson,
        ContactEmail: c.ContactEmail,
        ContactPhone: c.ContactPhone,
        Note: c.Note,
        IsActive: c.IsActive,
        CreatedAt: c.CreatedAt,
        UpdatedAt: c.UpdatedAt);
}
