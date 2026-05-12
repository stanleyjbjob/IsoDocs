using FluentAssertions;
using IsoDocs.Domain.Common;
using IsoDocs.Domain.Identity;

namespace IsoDocs.Application.UnitTests.Identity.Delegations;

public class DelegationDomainTests
{
    private static Delegation CreateDelegation(DateTimeOffset start, DateTimeOffset end)
        => new(
            id: Guid.NewGuid(),
            delegatorUserId: Guid.NewGuid(),
            delegateUserId: Guid.NewGuid(),
            startAt: start,
            endAt: end,
            note: null);

    [Fact]
    public void IsEffectiveAt_Within_Period_Returns_True()
    {
        var now = DateTimeOffset.UtcNow;
        var d = CreateDelegation(now.AddHours(-1), now.AddHours(1));
        d.IsEffectiveAt(now).Should().BeTrue();
    }

    [Fact]
    public void IsEffectiveAt_Before_Start_Returns_False()
    {
        var now = DateTimeOffset.UtcNow;
        var d = CreateDelegation(now.AddHours(1), now.AddHours(2));
        d.IsEffectiveAt(now).Should().BeFalse();
    }

    [Fact]
    public void IsEffectiveAt_After_End_Returns_False()
    {
        var now = DateTimeOffset.UtcNow;
        var d = CreateDelegation(now.AddHours(-2), now.AddHours(-1));
        d.IsEffectiveAt(now).Should().BeFalse();
    }

    [Fact]
    public void IsEffectiveAt_Revoked_Returns_False()
    {
        var now = DateTimeOffset.UtcNow;
        var d = CreateDelegation(now.AddHours(-1), now.AddHours(1));
        d.Revoke();
        d.IsEffectiveAt(now).Should().BeFalse();
    }

    [Fact]
    public void Create_With_End_Before_Start_Throws_DomainException()
    {
        var now = DateTimeOffset.UtcNow;
        var act = () => new Delegation(
            id: Guid.NewGuid(),
            delegatorUserId: Guid.NewGuid(),
            delegateUserId: Guid.NewGuid(),
            startAt: now.AddHours(1),
            endAt: now,
            note: null);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("delegation.invalid_period");
    }

    [Fact]
    public void Create_Self_Delegation_Throws_DomainException()
    {
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var act = () => new Delegation(
            id: Guid.NewGuid(),
            delegatorUserId: userId,
            delegateUserId: userId,
            startAt: now,
            endAt: now.AddHours(1),
            note: null);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("delegation.self_delegation");
    }

    [Fact]
    public void Revoke_Sets_IsRevoked_True()
    {
        var now = DateTimeOffset.UtcNow;
        var d = CreateDelegation(now.AddHours(-1), now.AddHours(1));
        d.IsRevoked.Should().BeFalse();
        d.Revoke();
        d.IsRevoked.Should().BeTrue();
    }
}
