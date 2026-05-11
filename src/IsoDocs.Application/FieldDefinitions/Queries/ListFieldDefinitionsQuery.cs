using IsoDocs.Application.Common.Messaging;

namespace IsoDocs.Application.FieldDefinitions.Queries;

public sealed record ListFieldDefinitionsQuery(bool IncludeInactive = false)
    : IQuery<IReadOnlyList<FieldDefinitionDto>>;

public sealed class ListFieldDefinitionsQueryHandler
    : IQueryHandler<ListFieldDefinitionsQuery, IReadOnlyList<FieldDefinitionDto>>
{
    private readonly IFieldDefinitionRepository _repo;

    public ListFieldDefinitionsQueryHandler(IFieldDefinitionRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<FieldDefinitionDto>> Handle(
        ListFieldDefinitionsQuery request, CancellationToken cancellationToken)
    {
        var list = await _repo.ListAsync(request.IncludeInactive, cancellationToken);
        return list.Select(FieldDefinitionDtoMapper.ToDto).ToList();
    }
}
