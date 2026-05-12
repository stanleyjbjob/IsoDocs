using FluentAssertions;
using IsoDocs.Application.Identity.Delegations;
using IsoDocs.Application.Identity.Delegations.Commands;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Domain.Common;
using IsoDocs.Domain.Identity;

namespace IsoDocs.Application.UnitTests.Identity.Delegations;

public class RevokeDelegationCommandHandlerTests
{
    private static Delegation CreateDelegation(Guid delegatorUserId)
    {
        var now = DateTimeOffset.UtcNow;
        return new Delegation(
            id: Guid.NewGuid(),
            delegatorUserId: delegatorUserId,
            delegateUserId: Guid.NewGuid(),
            startAt: now,
            endAt: now.AddDays(7),
            note: null);
    }

    [Fact]
    public async Task Owner_Can_Revoke_Own_Delegation()
    {
        var repo = new FakeDelegationRepository();
        var delegatorId = Guid.NewGuid();
        var delegation = CreateDelegation(delegatorId);
        await repo.AddAsync(delegation);
        var handler = new RevokeDelegationCommandHandler(repo);

        var cmd = new RevokeDelegationCommand(
            DelegationId: delegation.Id,
            RequesterUserId: delegatorId,
            IsAdmin: false);

        await handler.Handle(cmd, CancellationToken.None);

        delegation.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Admin_Can_Revoke_Others_Delegation()
    {
        var repo = new FakeDelegationRepository();
        var delegation = CreateDelegation(Guid.NewGuid());
        await repo.AddAsync(delegation);
        var handler = new RevokeDelegationCommandHandler(repo);

        var cmd = new RevokeDelegationCommand(
            DelegationId: delegation.Id,
            RequesterUserId: Guid.NewGuid(),
            IsAdmin: true);

        await handler.Handle(cmd, CancellationToken.None);

        delegation.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Non_Owner_Cannot_Revoke_Delegation()
    {
        var repo = new FakeDelegationRepository();
        var delegation = CreateDelegation(Guid.NewGuid());
        await repo.AddAsync(delegation);
        var handler = new RevokeDelegationCommandHandler(repo);

        var cmd = new RevokeDelegationCommand(
            DelegationId: delegation.Id,
            RequesterUserId: Guid.NewGuid(),
            IsAdmin: false);

        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        (await act.Should().ThrowAsync<DomainException>()).Which.Code.Should().Be(DelegationErrorCodes.NotOwner);
    }

    [Fact]
    public async Task Revoke_Nonexistent_Throws_NotFound()
    {
        var repo = new FakeDelegationRepository();
        var handler = new RevokeDelegationCommandHandler(repo);

        var cmd = new RevokeDelegationCommand(
            DelegationId: Guid.NewGuid(),
            RequesterUserId: Guid.NewGuid(),
            IsAdmin: false);

        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        (await act.Should().ThrowAsync<DomainException>()).Which.Code.Should().Be(DelegationErrorCodes.NotFound);
    }

    [Fact]
    public async Task Revoke_Already_Revoked_Throws_AlreadyRevoked()
    {
        var repo = new FakeDelegationRepository();
        var delegatorId = Guid.NewGuid();
        var delegation = CreateDelegation(delegatorId);
        delegation.Revoke();
        await repo.AddAsync(delegation);
        var handler = new RevokeDelegationCommandHandler(repo);

        var cmd = new RevokeDelegationCommand(
            DelegationId: delegation.Id,
            RequesterUserId: delegatorId,
            IsAdmin: false);

        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        (await act.Should().ThrowAsync<DomainException>()).Which.Code.Should().Be(DelegationErrorCodes.AlreadyRevoked);
    }
}
