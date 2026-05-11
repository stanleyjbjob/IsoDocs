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

public class RejectCaseCommandHandlerTests
{
    private static Case CreateCase()
        => new(Guid.NewGuid(), "ITCT-F01-260001", "測試案件",
            Guid.NewGuid(), Guid.NewGuid(), 1, 1,
            Guid.NewGuid(), null, null);

    private static CaseNode CreateCompletedNode(Guid caseId, int order)
    {
        var node = new CaseNode(Guid.NewGuid(), caseId, Guid.NewGuid(), order, $"節點{order}", null, null);
        node.Accept(Guid.NewGuid());
        node.Complete();
        return node;
    }

    private static CaseNode CreateInProgressNode(Guid caseId, int order)
    {
        var node = new CaseNode(Guid.NewGuid(), caseId, Guid.NewGuid(), order, $"節點{order}", null, null);
        node.Accept(Guid.NewGuid());
        return node;
    }

    [Fact]
    public async Task Reject_Should_Return_Current_Node_And_Reactivate_Previous()
    {
        var repo = new FakeCaseRepository();
        var @case = repo.SeedCase(CreateCase());
        var prevNode = repo.SeedNode(@case.Id, CreateCompletedNode(@case.Id, order: 1));
        var currNode = repo.SeedNode(@case.Id, CreateInProgressNode(@case.Id, order: 2));
        var publisher = Substitute.For<IPublisher>();
        var handler = new RejectCaseCommandHandler(repo, publisher);

        await handler.Handle(new RejectCaseCommand(@case.Id, Guid.NewGuid(), "退回原因"), CancellationToken.None);

        currNode.Status.Should().Be(CaseNodeStatus.Returned);
        prevNode.Status.Should().Be(CaseNodeStatus.Pending);
        repo.Actions.Should().ContainSingle(a => a.ActionType == CaseActionType.Reject);
    }

    [Fact]
    public async Task Reject_First_Node_Should_Return_Node_Without_Reactivation()
    {
        var repo = new FakeCaseRepository();
        var @case = repo.SeedCase(CreateCase());
        var currNode = repo.SeedNode(@case.Id, CreateInProgressNode(@case.Id, order: 1));
        var publisher = Substitute.For<IPublisher>();
        var handler = new RejectCaseCommandHandler(repo, publisher);

        await handler.Handle(new RejectCaseCommand(@case.Id, Guid.NewGuid(), null), CancellationToken.None);

        currNode.Status.Should().Be(CaseNodeStatus.Returned);
        @case.Status.Should().Be(CaseStatus.InProgress);
    }

    [Fact]
    public async Task Reject_Should_Throw_When_No_Active_Node()
    {
        var repo = new FakeCaseRepository();
        var @case = repo.SeedCase(CreateCase());
        var publisher = Substitute.For<IPublisher>();
        var handler = new RejectCaseCommandHandler(repo, publisher);

        var act = async () => await handler.Handle(
            new RejectCaseCommand(@case.Id, Guid.NewGuid(), null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*進行中的節點*");
    }

    [Fact]
    public async Task Reject_Should_Publish_Notification()
    {
        var repo = new FakeCaseRepository();
        var @case = repo.SeedCase(CreateCase());
        repo.SeedNode(@case.Id, CreateInProgressNode(@case.Id, order: 1));
        var publisher = Substitute.For<IPublisher>();
        var handler = new RejectCaseCommandHandler(repo, publisher);
        var actorId = Guid.NewGuid();

        await handler.Handle(new RejectCaseCommand(@case.Id, actorId, null), CancellationToken.None);

        await publisher.Received(1).Publish(
            Arg.Is<CaseActionNotification>(n =>
                n.CaseId == @case.Id && n.ActionType == CaseActionType.Reject && n.ActorUserId == actorId),
            Arg.Any<CancellationToken>());
    }
}
