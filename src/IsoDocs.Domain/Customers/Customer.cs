using IsoDocs.Domain.Common;

namespace IsoDocs.Domain.Customers;

public class Customer : Entity<Guid>, IAggregateRoot
{
    public string Code { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? ContactPerson { get; protected set; }
    public string? ContactEmail { get; protected set; }
    public string? ContactPhone { get; protected set; }
    public string? Note { get; protected set; }
    public bool IsActive { get; protected set; } = true;

    private Customer() { }

    public Customer(Guid id, string code, string name)
    {
        Id = id;
        Code = code;
        Name = name;
    }

    public void Update(string name, string? contactPerson, string? contactEmail, string? contactPhone, string? note)
    {
        Name = name;
        ContactPerson = contactPerson;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
        Note = note;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
