using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.DocumentTypes;

internal static class DocumentTypeDtoMapper
{
    internal static DocumentTypeDto ToDto(DocumentType d) => new(
        d.Id,
        d.CompanyCode,
        d.Code,
        d.Name,
        d.SequenceYear,
        d.CurrentSequence,
        d.IsActive,
        d.CreatedAt,
        d.UpdatedAt);
}
