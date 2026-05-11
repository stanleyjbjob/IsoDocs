using FluentAssertions;
using IsoDocs.Application.Cases.Commands;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Common;

namespace IsoDocs.Application.UnitTests.Cases;

public sealed class SpawnChildCaseCommandTests
{
    private static Case CreateParentCase(CaseStatus status = CaseStatus.InProgress)
    {
        var c = new Case(
            id: Guid.NewGuid(),
            caseNumber: "ITCT-F01-260001",
            title: "主需求單",
            documentTypeId: Guid.NewGuid(),
            workflowTemplateId: Guid.NewGuid(),
            templateVersion: 1,
            fieldVersion: 1,
            initiatedByUserId: Guid.NewGuid(),
            expectedCompletionAt: null,
            customerId: null);
        if (status == CaseStatus.Closed) c.Close();
        if (status == CaseStatus.Voided) c.Void();
        return c;
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesChildCaseAndRelation()
    {
        var caseRepo = new FakeCaseRepository();
        var relationRepo = new FakeCaseRelationRepository();
        var parent = CreateParentCase();
        caseRepo.Seed(parent);
        var userId = Guid.NewGuid();

        var handler = new SpawnChildCaseCommandHandler(caseRepo, relationRepo);
        var result = await handler.Handle(
            new SpawnChildCaseCommand(parent.Id, "子流程：規格變更", userId),
            CancellationToken.None);

        result.ChildCaseId.Should().NotBeEmpty();
        result.CaseNumber.Should().StartWith(parent.CaseNumber + "-C");
        result.RelationId.Should().NotBeEmpty();

        caseRepo.Store.Should().ContainKey(result.ChildCaseId);
        relationRepo.Store.Should().HaveCount(1);

        var relation = relationRepo.Store[0];
        relation.ParentCaseId.Should().Be(parent.Id);
        relation.ChildCaseId.Should().Be(result.ChildCaseId);
        relation.RelationType.Should().Be(CaseRelationType.Subprocess);
        relation.CreatedByUserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_ParentNotFound_ThrowsDomainException()
    {
        var handler = new SpawnChildCaseCommandHandler(
            new FakeCaseRepository(),
            new FakeCaseRelationRepository());

        var act = () => handler.Handle(
            new SpawnChildCaseCommand(Guid.NewGuid(), "子流程", Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*找不到案件*");
    }

    [Fact]
    public async Task Handle_ParentClosed_ThrowsDomainException()
    {
        var caseRepo = new FakeCaseRepository();
        var parent = CreateParentCase(CaseStatus.Closed);
        caseRepo.Seed(parent);

        var handler = new SpawnChildCaseCommandHandler(caseRepo, new FakeCaseRelationRepository());

        var act = () => handler.Handle(
            new SpawnChildCaseCommand(parent.Id, "子流程", Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*只有進行中的案件可衍生子流程*");
    }

    [Fact]
    public async Task Handle_ParentVoided_ThrowsDomainException()
    {
        var caseRepo = new FakeCaseRepository();
        var parent = CreateParentCase(CaseStatus.Voided);
        caseRepo.Seed(parent);

        var handler = new SpawnChildCaseCommandHandler(caseRepo, new FakeCaseRelationRepository());

        var act = () => handler.Handle(
            new SpawnChildCaseCommand(parent.Id, "子流程", Guid.NewGuid()),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*只有進行中的案件可衍生子流程*");
    }

    [Fact]
    public async Task Handle_ChildCaseInheritsParentMetadata()
    {
        var docTypeId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var parent = new Case(
            id: Guid.NewGuid(),
            caseNumber: "ITCT-F01-260002",
            title: "主需求單",
            documentTypeId: docTypeId,
            workflowTemplateId: templateId,
            templateVersion: 3,
            fieldVersion: 2,
            initiatedByUserId: Guid.NewGuid(),
            expectedCompletionAt: null,
            customerId: customerId);

        var caseRepo = new FakeCaseRepository();
        caseRepo.Seed(parent);
        var userId = Guid.NewGuid();

        var handler = new SpawnChildCaseCommandHandler(caseRepo, new FakeCaseRelationRepository());
        var result = await handler.Handle(
            new SpawnChildCaseCommand(parent.Id, "子流程標題", userId),
            CancellationToken.None);

        var child = caseRepo.Store[result.ChildCaseId];
        child.DocumentTypeId.Should().Be(docTypeId);
        child.WorkflowTemplateId.Should().Be(templateId);
        child.TemplateVersion.Should().Be(3);
        child.FieldVersion.Should().Be(2);
        child.CustomerId.Should().Be(customerId);
        child.Status.Should().Be(CaseStatus.InProgress);
    }
}
