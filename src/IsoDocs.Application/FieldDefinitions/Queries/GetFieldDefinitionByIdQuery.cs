using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;

namespace IsoDocs.Application.FieldDefinitions.Queries;

public sealed record GetFieldDefinitionByIdQuery(Guid Id) : IQuery<FieldDefinitionDto>;

public sealed class GetFieldDefinitionByIdQueryHandler
    : IQueryHandler<GetFieldDefinitionByIdQuery, FieldDefinitionDto>
{
    private readonly IFieldDefinitionRepository _repo;

    public GetFieldDefinitionByIdQueryHandler(IFieldDefinitionRepository repo) => _repo = repo;

    public async Task<FieldDefinitionDto> Handle(
        GetFieldDefinitionByIdQuery request, CancellationToken cancellationToken)
    {
        var fd = await _repo.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new DomainException(FieldDefinitionErrorCodes.NotFound,
                $"欄位定義 '{request.Id}' 不存在。");
        return FieldDefinitionDtoMapper.ToDto(fd);
    }
}
