using IsoDocs.Domain.Identity;

namespace IsoDocs.Application.Identity.Delegations;

/// <summary>
/// 代理設定資料存取抽象（issue #30 [2.3.2]）。
/// </summary>
public interface IDelegationRepository
{
    Task<Delegation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>列出指定委託人的所有代理設定（含已撤銷）。</summary>
    Task<IReadOnlyList<Delegation>> ListByDelegatorAsync(Guid delegatorUserId, CancellationToken cancellationToken = default);

    /// <summary>取得在指定時間點對指定委託人生效的代理設定。</summary>
    Task<Delegation?> GetEffectiveByDelegatorAsync(Guid delegatorUserId, DateTimeOffset moment, CancellationToken cancellationToken = default);

    Task AddAsync(Delegation delegation, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
