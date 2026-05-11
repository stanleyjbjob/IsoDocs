namespace IsoDocs.Application.Customers;

public sealed record CustomerDto(
    Guid Id,
    string Code,
    string Name,
    string? ContactPerson,
    string? ContactEmail,
    string? ContactPhone,
    string? Note,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
