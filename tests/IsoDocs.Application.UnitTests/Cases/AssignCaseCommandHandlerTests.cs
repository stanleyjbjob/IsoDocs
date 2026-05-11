using FluentAssertions;
using IsoDocs.Application.Cases.Commands;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Domain.Common;
using MediatR;
using NSubstitute;

namespace IsoDocs.Application.UnitTests.Cases;

public class AssignCaseCommandHandlerTests
{
    private readonly FakeCaseRepository _caseRepo = new();
    private readonly FakeDocumentTypeRepository _docTypeRepo = new();
    private readonly FakeWorkflowRepository _workflowRepo = new();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private async Task<CaseDto> SeedCase()
    {
        var docType = _docTypeRepo.Seed();
        var template = _workflowRepo.SeedTemplate();
        _workflowRepo.SeedNode(template.Id, template.Version);
        var createHandler = new CreateCaseCommandHandler(_caseRepo, _docTypeRepo, _workflowRepo, _publisher);
        return await createHandler.Handle(
            new CreateCaseCommand("測試", docType.Id, template.Id, null, null, Guid.NewGuid()),
            CancellationToken.None);
    }

    private AssignCaseCommandHandler CreateHandler() => new(_caseRepo, _publisher);

    [Fact]
    public async Task Handle_ValidAssign_ShouldUpdateNodeAssignee()
    {
        var caseDto = await SeedCase();
        var assigneeId = Guid.NewGuid();

        await CreateHandler().Handle(
            new AssignCaseCommand(caseDto.Id, assigneeId), CancellationToken.None);

        _caseRepo.NodeStore[0].AssigneeUserId.Should().Be(assigneeId);
    }

    [Fact]
    public async Task Handle_CaseNotFound_ShouldThrowDomainException()
    {
        var act = async () => await CreateHandler().Handle(
            new AssignCaseCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*案件*");
    }

    [Fact]
    public async Task Handle_ShouldPublishCaseNodeAssignedNotification()
    {
        var caseDto = await SeedCase();
        var publisher2 = Substitute.For<IPublisher>();
        var handler = new AssignCaseCommandHandler(_caseRepo, publisher2);

        await handler.Handle(
            new AssignCaseCommand(caseDto.Id, Guid.NewGuid()), CancellationToken.None);

        await publisher2.Received(1).Publish(
            Arg.Any<CaseNodeAssignedNotification>(), Arg.Any<CancellationToken>());
    }
}
