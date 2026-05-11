using FluentAssertions;
using IsoDocs.Application.Cases.Commands;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Domain.Common;
using MediatR;
using NSubstitute;

namespace IsoDocs.Application.UnitTests.Cases;

public class CreateCaseCommandHandlerTests
{
    private readonly FakeCaseRepository _caseRepo = new();
    private readonly FakeDocumentTypeRepository _docTypeRepo = new();
    private readonly FakeWorkflowRepository _workflowRepo = new();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private CreateCaseCommandHandler CreateHandler() =>
        new(_caseRepo, _docTypeRepo, _workflowRepo, _publisher);

    [Fact]
    public async Task Handle_ValidInput_ShouldCreateCaseAndInitialNode()
    {
        var docType = _docTypeRepo.Seed();
        var template = _workflowRepo.SeedTemplate();
        _workflowRepo.SeedNode(template.Id, template.Version);
        var userId = Guid.NewGuid();

        var cmd = new CreateCaseCommand(
            Title: "測試案件",
            DocumentTypeId: docType.Id,
            WorkflowTemplateId: template.Id,
            ExpectedCompletionAt: null,
            CustomerId: null,
            InitiatedByUserId: userId);

        var dto = await CreateHandler().Handle(cmd, CancellationToken.None);

        dto.Should().NotBeNull();
        dto.Title.Should().Be("測試案件");
        dto.InitiatedByUserId.Should().Be(userId);
        dto.Status.Should().Be("InProgress");
        dto.CaseNumber.Should().NotBeEmpty();
        _caseRepo.CaseStore.Should().ContainKey(dto.Id);
        _caseRepo.NodeStore.Should().HaveCount(1);
        _caseRepo.NodeStore[0].CaseId.Should().Be(dto.Id);
    }

    [Fact]
    public async Task Handle_CaseNumber_ShouldFollowDocumentTypeFormat()
    {
        var docType = _docTypeRepo.Seed("ITCT", "F01");
        var template = _workflowRepo.SeedTemplate();
        _workflowRepo.SeedNode(template.Id, template.Version);

        var dto = await CreateHandler().Handle(
            new CreateCaseCommand("A", docType.Id, template.Id, null, null, Guid.NewGuid()),
            CancellationToken.None);

        dto.CaseNumber.Should().StartWith("ITCT-F01-");
    }

    [Fact]
    public async Task Handle_DocumentTypeNotFound_ShouldThrowDomainException()
    {
        var template = _workflowRepo.SeedTemplate();
        var cmd = new CreateCaseCommand("T", Guid.NewGuid(), template.Id, null, null, Guid.NewGuid());

        var act = async () => await CreateHandler().Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*文件類型*");
    }

    [Fact]
    public async Task Handle_WorkflowNotFound_ShouldThrowDomainException()
    {
        var docType = _docTypeRepo.Seed();
        var cmd = new CreateCaseCommand("T", docType.Id, Guid.NewGuid(), null, null, Guid.NewGuid());

        var act = async () => await CreateHandler().Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*流程範本*");
    }

    [Fact]
    public async Task Handle_NoWorkflowNodes_ShouldThrowDomainException()
    {
        var docType = _docTypeRepo.Seed();
        var template = _workflowRepo.SeedTemplate();

        var cmd = new CreateCaseCommand("T", docType.Id, template.Id, null, null, Guid.NewGuid());
        var act = async () => await CreateHandler().Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*節點*");
    }

    [Fact]
    public async Task Handle_ShouldPublishCaseInitiatedNotification()
    {
        var docType = _docTypeRepo.Seed();
        var template = _workflowRepo.SeedTemplate();
        _workflowRepo.SeedNode(template.Id, template.Version);

        await CreateHandler().Handle(
            new CreateCaseCommand("A", docType.Id, template.Id, null, null, Guid.NewGuid()),
            CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Any<CaseInitiatedNotification>(), Arg.Any<CancellationToken>());
    }
}
