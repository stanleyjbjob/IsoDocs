using IsoDocs.Application.Common.Messaging;

namespace IsoDocs.Application.Cases.Queries;

public sealed record GetMyTodosQuery(Guid UserId) : IQuery<IReadOnlyList<TodoItemDto>>;

public sealed class GetMyTodosQueryHandler : IQueryHandler<GetMyTodosQuery, IReadOnlyList<TodoItemDto>>
{
    private readonly ICaseDashboardService _dashboard;

    public GetMyTodosQueryHandler(ICaseDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    public Task<IReadOnlyList<TodoItemDto>> Handle(GetMyTodosQuery request, CancellationToken cancellationToken)
        => _dashboard.GetMyTodosAsync(request.UserId, cancellationToken);
}
