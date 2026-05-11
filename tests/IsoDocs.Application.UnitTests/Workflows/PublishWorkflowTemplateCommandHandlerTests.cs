using FluentAssertions;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Application.Workflows.Commands;
using IsoDocs.Domain.Common;
using Xunit;

namespace IsoDocs.Application.UnitTests.Workflows;

public class PublishWorkflowTemplateCommandHandlerTests
{
    [Fact]
    public async Task Publish_Unpublished_Template_Should_Set_PublishedAt()
    {
        var repo = new FakeWorkflowTemplateRepository();
        var template = repo.Seed("PUB-001", "待發行範本");
        var handler = new PublishWorkflowTemplateCommandHandler(repo);

        var dto = await handler.Handle(new PublishWorkflowTemplateCommand(template.Id), CancellationToken.None);

        dto.PublishedAt.Should().NotBeNull();
        dto.Version.Should().Be(1);
    }

    [Fact]
    public async Task Publish_Already_Published_Template_Should_Throw_DomainException()
    {
        var repo = new FakeWorkflowTemplateRepository();
        var template = repo.Seed("PUB-002", "已發行範本");
        template.Publish();
        var handler = new PublishWorkflowTemplateCommandHandler(repo);

        var act = async () => await handler.Handle(new PublishWorkflowTemplateCommand(template.Id), CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("WORKFLOW_TEMPLATE/NOT_PUBLISHABLE");
    }

    [Fact]
    public async Task Publish_Nonexistent_Template_Should_Throw_DomainException()
    {
        var repo = new FakeWorkflowTemplateRepository();
        var handler = new PublishWorkflowTemplateCommandHandler(repo);

        var act = async () => await handler.Handle(new PublishWorkflowTemplateCommand(Guid.NewGuid()), CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("WORKFLOW_TEMPLATE/NOT_FOUND");
    }
}
