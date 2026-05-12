namespace IsoDocs.Application.Identity.Delegations;

/// <summary>代理設定的 API 回應 DTO。</summary>
public sealed record DelegationDto(
    Guid Id,
    Guid DelegatorUserId,
    Guid DelegateUserId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string? Note,
    bool IsRevoked,
    bool IsCurrentlyEffective);
