using IsoDocs.Domain.Common;

namespace IsoDocs.Domain.Identity;

/// <summary>
/// 代理機制（issue [2.3.2]）。指定期間內，由代理人 (DelegateUserId) 代為處理委託人 (DelegatorUserId) 的待辦事項。
/// 期間結束後自動失效。
/// </summary>
public class Delegation : Entity<Guid>, IAggregateRoot
{
    public Guid DelegatorUserId { get; protected set; }
    public Guid DelegateUserId { get; protected set; }
    public DateTimeOffset StartAt { get; protected set; }
    public DateTimeOffset EndAt { get; protected set; }
    public string? Note { get; protected set; }
    public bool IsRevoked { get; protected set; }

    private Delegation() { }

    public Delegation(Guid id, Guid delegatorUserId, Guid delegateUserId, DateTimeOffset startAt, DateTimeOffset endAt, string? note)
    {
        if (endAt <= startAt)
            throw new DomainException("delegation.invalid_period", "代理結束時間必須晚於開始時間");
        if (delegatorUserId == delegateUserId)
            throw new DomainException("delegation.self_delegation", "不可指派自己為代理人");

        Id = id;
        DelegatorUserId = delegatorUserId;
        DelegateUserId = delegateUserId;
        StartAt = startAt;
        EndAt = endAt;
        Note = note;
    }

    public bool IsEffectiveAt(DateTimeOffset moment) =>
        !IsRevoked && moment >= StartAt && moment < EndAt;

    public void Revoke()
    {
        IsRevoked = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
