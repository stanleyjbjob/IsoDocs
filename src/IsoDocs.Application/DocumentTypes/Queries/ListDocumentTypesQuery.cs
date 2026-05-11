using IsoDocs.Application.Common.Messaging;

namespace IsoDocs.Application.DocumentTypes.Queries;

public sealed record ListDocumentTypesQuery(bool IncludeInactive = true)
    : IQuery<IReadOnlyList<DocumentTypeDto>>;

public sealed class ListDocumentTypesQueryHandler
    : IQueryHandler<ListDocumentTypesQuery, IReadOnlyList<DocumentTypeDto>>
{
    private readonly IDocumentTypeRepository _repo;

    public ListDocumentTypesQueryHandler(IDocumentTypeRepository repo)
    {
        _repo = repo;
    }

    public async Task<IReadOnlyList<DocumentTypeDto>> Handle(
        ListDocumentTypesQuery request, CancellationToken cancellationToken)
    {
        var list = await _repo.ListAsync(request.IncludeInactive, cancellationToken);
        return list.Select(DocumentTypeDtoMapper.ToDto).ToList();
    }
}
