using FluentAssertions;
using IsoDocs.Application.Cases;
using IsoDocs.Application.Cases.Commands;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Common;
using MediatR;
using NSubstitute;
using Xunit;

namespace IsoDocs.Application.UnitTests.Cases;

public class AcceptCaseCommandHandlerTests
{
    private static Case CreateCase()
        => new(Guid.NewGuid(), "ITCT-F01-260001", "測試案件",
            Guid.NewGuid(), Guid.NewGuid(), 1, 1,
            Guid.NewGuid(), null, null);

    private static CaseNode CreatePendingNode(Guid caseId, int order = 1)
        => new(Guid.NewGuid(), caseId, Guid.NewGuid(), order, $"節點{order}", null, null);

    [Fact]
    public async Task Accept_Should_Transition_Node_To_InProgress_And_Write_Action()
    {
        var repo = new FakeCaseRepository();
        var @case = repo.SeedCase(CreateCase());
        var node = repo.SeedNode(@case.Id, CreatePendingNode(@case.Id));
        var publisher = Substitute.For<IPublisher>();
        var handler = new AcceptCaseCommandHandler(repo, publisher);
        var actorId = Guid.NewGuid();

        await handler.Handle(new AcceptCaseCommand(@case.Id, actorId, "接單備註"), CancellationToken.None);

        node.Status.Should().Be(CaseNodeStatus.InProgress);
        node.AssigneeUserId.Should().Be(actorId);
        repo.Actions.Should().ContainSingle(a => a.ActionType == CaseActionType.Accept);
        await publisher.Received(1).Publish(
            Arg.Is<CaseActionNotification>(n =>
                n.CaseId == @case.Id && n.ActionType == CaseActionType.Accept),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Accept_Should_Throw_When_Case_Not_Found()
    {
        var repo = new FakeCaseRepository();
        var publisher = Substitute.For<IPublisher>();
        var handler = new AcceptCaseCommandHandler(repo, publisher);

        var act = async () => await handler.Handle(
            new AcceptCaseCommand(Guid.NewGuid(), Guid.NewGuid(), null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*不存在*");
    }

    [Fact]
    public async Task Accept_Should_Throw_When_No_Pending_Node()
    {
        var repo = new FakeCaseRepository();
        var @case = repo.SeedCase(CreateCase());
        var publisher = Substitute.For<IPublisher>();
        var handler = new AcceptCaseCommandHandler(repo, publisher);

        var act = async () => await handler.Handle(
            new AcceptCaseCommand(@case.Id, Guid.NewGuid(), null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*待處理*");
    }

    [Fact]
    public async Task Accept_Should_Pick_Lowest_Order_Pending_Node()
    {
        var repo = new FakeCaseRepository();
        var @case = repo.SeedCase(CreateCase());
        var node1 = repo.SeedNode(@case.Id, CreatePendingNode(@case.Id, order: 1));
        var node2 = repo.SeedNode(@case.Id, CreatePendingNode(@case.Id, order: 2));
        var publisher = Substitute.For<IPublisher>();
        var handler = new AcceptCaseCommandHandler(repo, publisher);

        await handler.Handle(new AcceptCaseCommand(@case.Id, Guid.NewGuid(), null), CancellationToken.None);

        node1.Status.Should().Be(CaseNodeStatus.InProgress);
        node2.Status.Should().Be(CaseNodeStatus.Pending);
    }
}
