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

public class ReplyCaseCommandHandlerTests
{
    private static Case CreateCase()
        => new(Guid.NewGuid(), "ITCT-F01-260001", "測試案件",
            Guid.NewGuid(), Guid.NewGuid(), 1, 1,
            Guid.NewGuid(), null, null);

    private static CaseNode CreateInProgressNode(Guid caseId, int order = 1)
    {
        var node = new CaseNode(Guid.NewGuid(), caseId, Guid.NewGuid(), order, $"節點{order}", null, null);
        node.Accept(Guid.NewGuid());
        return node;
    }

    private static CaseNode CreatePendingNode(Guid caseId, int order)
        => new(Guid.NewGuid(), caseId, Guid.NewGuid(), order, $"節點{order}", null, null);

    [Fact]
    public async Task ReplyClose_Should_Complete_Node_And_Write_Action()
    {
        var repo = new FakeCaseRepository();
        var @case = repo.SeedCase(CreateCase());
        var node = repo.SeedNode(@case.Id, CreateInProgressNode(@case.Id));
        var publisher = Substitute.For<IPublisher>();
        var handler = new ReplyCaseCommandHandler(repo, publisher);

        await handler.Handle(new ReplyCaseCommand(@case.Id, Guid.NewGuid(), "回覆結案"), CancellationToken.None);

        node.Status.Should().Be(CaseNodeStatus.Completed);
        @case.Status.Should().Be(CaseStatus.Closed);
        repo.Actions.Should().ContainSingle(a => a.ActionType == CaseActionType.ReplyClose);
    }

    [Fact]
    public async Task ReplyClose_With_Next_Node_Should_Not_Close_Case()
    {
        var repo = new FakeCaseRepository();
        var @case = repo.SeedCase(CreateCase());
        repo.SeedNode(@case.Id, CreateInProgressNode(@case.Id, order: 1));
        repo.SeedNode(@case.Id, CreatePendingNode(@case.Id, order: 2));
        var publisher = Substitute.For<IPublisher>();
        var handler = new ReplyCaseCommandHandler(repo, publisher);

        await handler.Handle(new ReplyCaseCommand(@case.Id, Guid.NewGuid(), null), CancellationToken.None);

        @case.Status.Should().Be(CaseStatus.InProgress);
    }

    [Fact]
    public async Task ReplyClose_Should_Throw_When_No_Active_Node()
    {
        var repo = new FakeCaseRepository();
        var @case = repo.SeedCase(CreateCase());
        var publisher = Substitute.For<IPublisher>();
        var handler = new ReplyCaseCommandHandler(repo, publisher);

        var act = async () => await handler.Handle(
            new ReplyCaseCommand(@case.Id, Guid.NewGuid(), null), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*進行中的節點*");
    }
}
