using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;

namespace IsoDocs.Application.DocumentTypes.Queries;

public sealed record GetDocumentTypeByIdQuery(Guid Id) : IQuery<DocumentTypeDto>;

public sealed class GetDocumentTypeByIdQueryHandler
    : IQueryHandler<GetDocumentTypeByIdQuery, DocumentTypeDto>
{
    private readonly IDocumentTypeRepository _repo;

    public GetDocumentTypeByIdQueryHandler(IDocumentTypeRepository repo)
    {
        _repo = repo;
    }

    public async Task<DocumentTypeDto> Handle(
        GetDocumentTypeByIdQuery request, CancellationToken cancellationToken)
    {
        var dt = await _repo.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new DomainException(DocumentTypeErrorCodes.NotFound,
                $"DocumentType '{request.Id}' 不存在。");

        return DocumentTypeDtoMapper.ToDto(dt);
    }
}
