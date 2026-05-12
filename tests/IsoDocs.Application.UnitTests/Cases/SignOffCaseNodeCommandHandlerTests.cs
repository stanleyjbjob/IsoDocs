using FluentAssertions;
using IsoDocs.Application.Cases.Commands;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Common;
using Xunit;

namespace IsoDocs.Application.UnitTests.Cases;

public class SignOffCaseNodeCommandHandlerTests
{
    private static Case MakeCase(bool voided = false)
    {
        var c = new Case(
            id: Guid.NewGuid(),
            caseNumber: "TEST-001",
            title: "測試案件",
            documentTypeId: Guid.NewGuid(),
            workflowTemplateId: Guid.NewGuid(),
            templateVersion: 1,
            fieldVersion: 1,
            initiatedByUserId: Guid.NewGuid(),
            expectedCompletionAt: null,
            customerId: null);
        if (voided) c.Void();
        return c;
    }

    private static CaseNode MakeNode(Guid caseId, string nodeName = "文件發行核准")
    {
        return new CaseNode(
            id: Guid.NewGuid(),
            caseId: caseId,
            workflowNodeId: Guid.NewGuid(),
            nodeOrder: 1,
            nodeName: nodeName,
            assigneeUserId: null,
            originalExpectedAt: null);
    }

    [Fact]
    public async Task SignOff_ValidInput_Should_PersistAction_And_ReturnDto()
    {
        var repo = new FakeCaseRepository();
        var @case = MakeCase();
        var node = MakeNode(@case.Id);
        repo.SeedCase(@case);
        repo.SeedNode(node);
        var actorId = Guid.NewGuid();
        var handler = new SignOffCaseNodeCommandHandler(repo);

        var dto = await handler.Handle(
            new SignOffCaseNodeCommand(@case.Id, node.Id, actorId, "核准，符合規範"),
            CancellationToken.None);

        dto.CaseId.Should().Be(@case.Id);
        dto.CaseNodeId.Should().Be(node.Id);
        dto.NodeName.Should().Be("文件發行核准");
        dto.ActorUserId.Should().Be(actorId);
        dto.Comment.Should().Be("核准，符合規範");
        repo.Actions.Should().ContainSingle(a => a.ActionType == CaseActionType.SignOff);
    }

    [Fact]
    public async Task SignOff_NullComment_Should_Succeed()
    {
        var repo = new FakeCaseRepository();
        var @case = MakeCase();
        var node = MakeNode(@case.Id);
        repo.SeedCase(@case);
        repo.SeedNode(node);
        var handler = new SignOffCaseNodeCommandHandler(repo);

        var dto = await handler.Handle(
            new SignOffCaseNodeCommand(@case.Id, node.Id, Guid.NewGuid(), null),
            CancellationToken.None);

        dto.Comment.Should().BeNull();
        repo.Actions.Should().HaveCount(1);
    }

    [Fact]
    public async Task SignOff_VoidedCase_Should_Throw_DomainException()
    {
        var repo = new FakeCaseRepository();
        var @case = MakeCase(voided: true);
        var node = MakeNode(@case.Id);
        repo.SeedCase(@case);
        repo.SeedNode(node);
        var handler = new SignOffCaseNodeCommandHandler(repo);

        var act = async () => await handler.Handle(
            new SignOffCaseNodeCommand(@case.Id, node.Id, Guid.NewGuid(), null),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("case.voided");
    }

    [Fact]
    public async Task SignOff_NonExistentCase_Should_Throw_DomainException()
    {
        var repo = new FakeCaseRepository();
        var handler = new SignOffCaseNodeCommandHandler(repo);

        var act = async () => await handler.Handle(
            new SignOffCaseNodeCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("case.not_found");
    }

    [Fact]
    public async Task SignOff_NodeBelongsToOtherCase_Should_Throw_DomainException()
    {
        var repo = new FakeCaseRepository();
        var @case = MakeCase();
        var node = MakeNode(caseId: Guid.NewGuid());
        repo.SeedCase(@case);
        repo.SeedNode(node);
        var handler = new SignOffCaseNodeCommandHandler(repo);

        var act = async () => await handler.Handle(
            new SignOffCaseNodeCommand(@case.Id, node.Id, Guid.NewGuid(), null),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("case_node.not_found");
    }
}
