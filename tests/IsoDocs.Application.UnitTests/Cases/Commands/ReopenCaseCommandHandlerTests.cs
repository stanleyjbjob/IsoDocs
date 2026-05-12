using FluentAssertions;
using IsoDocs.Application.Cases.Commands;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Common;
using Xunit;

namespace IsoDocs.Application.UnitTests.Cases.Commands;

public class ReopenCaseCommandHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid DocTypeId = Guid.NewGuid();
    private static readonly Guid TemplateId = Guid.NewGuid();

    private static (FakeCaseRepository cases, FakeCaseRelationRepository relations, ReopenCaseCommandHandler handler) Build()
    {
        var cases = new FakeCaseRepository();
        var relations = new FakeCaseRelationRepository();
        var handler = new ReopenCaseCommandHandler(cases, relations);
        return (cases, relations, handler);
    }

    [Fact]
    public async Task Reopen_Closed_Case_Should_Create_New_Case_And_Relation()
    {
        var (cases, relations, handler) = Build();
        var original = cases.Seed("CASE-001", "原始案件", DocTypeId, TemplateId, UserId, CaseStatus.Closed);

        var cmd = new ReopenCaseCommand(original.Id, "重開案件", UserId);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.NewCaseId.Should().NotBe(original.Id);
        result.CaseRelationId.Should().NotBeEmpty();

        cases.Store.Should().ContainKey(result.NewCaseId);
        var newCase = cases.Store[result.NewCaseId];
        newCase.Title.Should().Be("重開案件");
        newCase.DocumentTypeId.Should().Be(DocTypeId);
        newCase.WorkflowTemplateId.Should().Be(TemplateId);
        newCase.Status.Should().Be(CaseStatus.InProgress);
        newCase.InitiatedByUserId.Should().Be(UserId);

        relations.Store.Should().HaveCount(1);
        var relation = relations.Store[0];
        relation.ParentCaseId.Should().Be(original.Id);
        relation.ChildCaseId.Should().Be(result.NewCaseId);
        relation.RelationType.Should().Be(CaseRelationType.Reopen);
        relation.CreatedByUserId.Should().Be(UserId);
    }

    [Fact]
    public async Task Reopen_Non_Existing_Case_Should_Throw_DomainException()
    {
        var (_, _, handler) = Build();
        var cmd = new ReopenCaseCommand(Guid.NewGuid(), "重開案件", UserId);

        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("case.not_found");
    }

    [Fact]
    public async Task Reopen_InProgress_Case_Should_Throw_DomainException()
    {
        var (cases, _, handler) = Build();
        var inProgress = cases.Seed("CASE-002", "進行中案件", DocTypeId, TemplateId, UserId, CaseStatus.InProgress);

        var cmd = new ReopenCaseCommand(inProgress.Id, "嘗試重開", UserId);
        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("case.not_closed");
    }

    [Fact]
    public async Task Reopen_Voided_Case_Should_Throw_DomainException()
    {
        var (cases, _, handler) = Build();
        var voided = cases.Seed("CASE-003", "已作廢案件", DocTypeId, TemplateId, UserId, CaseStatus.Voided);

        var cmd = new ReopenCaseCommand(voided.Id, "嘗試重開", UserId);
        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("case.not_closed");
    }
}
