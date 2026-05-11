using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.FieldDefinitions;

public static class FieldDefinitionDtoMapper
{
    public static FieldDefinitionDto ToDto(FieldDefinition fd) => new(
        Id: fd.Id,
        Code: fd.Code,
        Name: fd.Name,
        Version: fd.Version,
        Type: fd.Type,
        TypeName: fd.Type.ToString(),
        IsRequired: fd.IsRequired,
        ValidationJson: fd.ValidationJson,
        OptionsJson: fd.OptionsJson,
        IsActive: fd.IsActive,
        CreatedAt: fd.CreatedAt,
        UpdatedAt: fd.UpdatedAt);
}
