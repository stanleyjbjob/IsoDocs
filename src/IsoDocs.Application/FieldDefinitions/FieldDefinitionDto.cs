using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.FieldDefinitions;

public sealed record FieldDefinitionDto(
    Guid Id,
    string Code,
    string Name,
    int Version,
    FieldType Type,
    string TypeName,
    bool IsRequired,
    string? ValidationJson,
    string? OptionsJson,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
