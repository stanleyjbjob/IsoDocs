using IsoDocs.Application.Common.Messaging;

namespace IsoDocs.Application.Identity.Delegations.Queries;

/// <summary>
/// 列出指定委託人的所有代理設定。對應 GET /api/delegations?delegatorUserId={id}。
/// 一般使用者只能查詢自己；管理者可查詢任何人。
/// </summary>
public sealed record ListDelegationsQuery(Guid DelegatorUserId) : IQuery<IReadOnlyList<DelegationDto>>;

public sealed class ListDelegationsQueryHandler : IQueryHandler<ListDelegationsQuery, IReadOnlyList<DelegationDto>>
{
    private readonly IDelegationRepository _delegations;

    public ListDelegationsQueryHandler(IDelegationRepository delegations)
    {
        _delegations = delegations;
    }

    public async Task<IReadOnlyList<DelegationDto>> Handle(ListDelegationsQuery request, CancellationToken cancellationToken)
    {
        var items = await _delegations.ListByDelegatorAsync(request.DelegatorUserId, cancellationToken);
        return items.Select(d => d.ToDto()).ToList();
    }
}
