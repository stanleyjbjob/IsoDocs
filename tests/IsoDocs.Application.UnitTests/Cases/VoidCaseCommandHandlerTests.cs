using FluentAssertions;
using IsoDocs.Application.Cases.Commands;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Common;

namespace IsoDocs.Application.UnitTests.Cases;

public class VoidCaseCommandHandlerTests
{
    private static readonly Guid ActorId = Guid.NewGuid();

    [Fact]
    public async Task Void_InProgress_Case_Should_Set_Status_Voided_And_Write_Action()
    {
        var repo = new FakeCaseRepository();
        var @case = repo.CreateInProgressCase();
        var handler = new VoidCaseCommandHandler(repo);
        var cmd = new VoidCaseCommand(@case.Id, ActorId, "測試作廢");

        await handler.Handle(cmd, CancellationToken.None);

        @case.Status.Should().Be(CaseStatus.Voided);
        @case.VoidedAt.Should().NotBeNull();
        repo.Actions.Should().ContainSingle(a =>
            a.CaseId == @case.Id && a.ActionType == CaseActionType.Void);
    }

    [Fact]
    public async Task Void_Case_With_InProgress_Children_Should_Cascade_Void_Children()
    {
        var repo = new FakeCaseRepository();
        var parent = repo.CreateInProgressCase();
        var child1 = repo.CreateInProgressCase();
        var child2 = repo.CreateInProgressCase();

        repo.SeedRelation(new CaseRelation(Guid.NewGuid(), parent.Id, child1.Id, CaseRelationType.Subprocess, ActorId));
        repo.SeedRelation(new CaseRelation(Guid.NewGuid(), parent.Id, child2.Id, CaseRelationType.Subprocess, ActorId));

        var handler = new VoidCaseCommandHandler(repo);
        await handler.Handle(new VoidCaseCommand(parent.Id, ActorId, null), CancellationToken.None);

        parent.Status.Should().Be(CaseStatus.Voided);
        child1.Status.Should().Be(CaseStatus.Voided);
        child2.Status.Should().Be(CaseStatus.Voided);

        repo.Actions.Should().Contain(a => a.CaseId == parent.Id && a.ActionType == CaseActionType.Void);
        repo.Actions.Should().Contain(a => a.CaseId == child1.Id && a.ActionType == CaseActionType.VoidCascade);
        repo.Actions.Should().Contain(a => a.CaseId == child2.Id && a.ActionType == CaseActionType.VoidCascade);
    }

    [Fact]
    public async Task Void_Case_Should_Skip_Already_Voided_Child()
    {
        var repo = new FakeCaseRepository();
        var parent = repo.CreateInProgressCase();
        var childAlreadyVoided = repo.CreateInProgressCase();
        childAlreadyVoided.Void();

        repo.SeedRelation(new CaseRelation(Guid.NewGuid(), parent.Id, childAlreadyVoided.Id, CaseRelationType.Subprocess, ActorId));

        var handler = new VoidCaseCommandHandler(repo);
        await handler.Handle(new VoidCaseCommand(parent.Id, ActorId, null), CancellationToken.None);

        // 已作廢的子流程不應再寫入 VoidCascade 軌跡
        repo.Actions.Should()
            .NotContain(a => a.CaseId == childAlreadyVoided.Id && a.ActionType == CaseActionType.VoidCascade);
    }

    [Fact]
    public async Task Void_Subprocess_Should_Not_Affect_Parent()
    {
        var repo = new FakeCaseRepository();
        var parent = repo.CreateInProgressCase();
        var child = repo.CreateInProgressCase();

        repo.SeedRelation(new CaseRelation(Guid.NewGuid(), parent.Id, child.Id, CaseRelationType.Subprocess, ActorId));

        var handler = new VoidCaseCommandHandler(repo);
        // 子流程獨立作廢
        await handler.Handle(new VoidCaseCommand(child.Id, ActorId, "子流程獨立作廢"), CancellationToken.None);

        child.Status.Should().Be(CaseStatus.Voided);
        // 主單不受影響
        parent.Status.Should().Be(CaseStatus.InProgress);
        repo.Actions.Should().ContainSingle(a => a.CaseId == child.Id && a.ActionType == CaseActionType.Void);
        repo.Actions.Should().NotContain(a => a.CaseId == parent.Id);
    }

    [Fact]
    public async Task Void_Nonexistent_Case_Should_Throw_DomainException()
    {
        var repo = new FakeCaseRepository();
        var handler = new VoidCaseCommandHandler(repo);
        var cmd = new VoidCaseCommand(Guid.NewGuid(), ActorId, null);

        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>()
            .Where(ex => ex.Code == "case.not_found");
    }

    [Fact]
    public async Task Void_Already_Voided_Case_Should_Throw_DomainException()
    {
        var repo = new FakeCaseRepository();
        var @case = repo.CreateInProgressCase();
        @case.Void();
        var handler = new VoidCaseCommandHandler(repo);
        var cmd = new VoidCaseCommand(@case.Id, ActorId, null);

        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>()
            .Where(ex => ex.Code == "case.already_voided");
    }
}
