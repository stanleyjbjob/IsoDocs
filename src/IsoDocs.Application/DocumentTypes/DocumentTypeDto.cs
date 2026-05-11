namespace IsoDocs.Application.DocumentTypes;

public sealed record DocumentTypeDto(
    Guid Id,
    string CompanyCode,
    string Code,
    string Name,
    int SequenceYear,
    int CurrentSequence,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
