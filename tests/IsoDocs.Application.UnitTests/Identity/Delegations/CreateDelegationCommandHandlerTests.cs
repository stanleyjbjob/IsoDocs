using FluentAssertions;
using IsoDocs.Application.Identity.Delegations.Commands;
using IsoDocs.Application.UnitTests.Fakes;

namespace IsoDocs.Application.UnitTests.Identity.Delegations;

public class CreateDelegationCommandHandlerTests
{
    [Fact]
    public async Task Create_Valid_Delegation_Persists_And_Returns_Id()
    {
        var repo = new FakeDelegationRepository();
        var handler = new CreateDelegationCommandHandler(repo);
        var now = DateTimeOffset.UtcNow;

        var cmd = new CreateDelegationCommand(
            DelegatorUserId: Guid.NewGuid(),
            DelegateUserId: Guid.NewGuid(),
            StartAt: now,
            EndAt: now.AddDays(7),
            Note: "出差代理");

        var id = await handler.Handle(cmd, CancellationToken.None);

        id.Should().NotBeEmpty();
        repo.Store.Should().HaveCount(1);
        repo.Store[0].Id.Should().Be(id);
        repo.Store[0].Note.Should().Be("出差代理");
    }

    [Fact]
    public async Task Create_Stores_Correct_Delegator_And_Delegate()
    {
        var repo = new FakeDelegationRepository();
        var handler = new CreateDelegationCommandHandler(repo);
        var now = DateTimeOffset.UtcNow;
        var delegatorId = Guid.NewGuid();
        var delegateId = Guid.NewGuid();

        var cmd = new CreateDelegationCommand(
            DelegatorUserId: delegatorId,
            DelegateUserId: delegateId,
            StartAt: now,
            EndAt: now.AddDays(3),
            Note: null);

        await handler.Handle(cmd, CancellationToken.None);

        repo.Store[0].DelegatorUserId.Should().Be(delegatorId);
        repo.Store[0].DelegateUserId.Should().Be(delegateId);
    }
}
