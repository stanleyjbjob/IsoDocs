using IsoDocs.Domain.Identity;

namespace IsoDocs.Application.Identity.Delegations;

public static class DelegationDtoMapper
{
    public static DelegationDto ToDto(this Delegation d) => new(
        Id: d.Id,
        DelegatorUserId: d.DelegatorUserId,
        DelegateUserId: d.DelegateUserId,
        StartAt: d.StartAt,
        EndAt: d.EndAt,
        Note: d.Note,
        IsRevoked: d.IsRevoked,
        IsCurrentlyEffective: d.IsEffectiveAt(DateTimeOffset.UtcNow));
}
