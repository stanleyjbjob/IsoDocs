using FluentAssertions;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Application.Workflows.Commands;
using IsoDocs.Domain.Common;
using IsoDocs.Domain.Workflows;
using Xunit;

namespace IsoDocs.Application.UnitTests.Workflows;

public class UpdateWorkflowTemplateCommandHandlerTests
{
    [Fact]
    public async Task Update_Unpublished_Template_Should_Keep_Version_And_Replace_Nodes()
    {
        var repo = new FakeWorkflowTemplateRepository();
        var template = repo.Seed("UPD-001", "原始範本");
        await repo.AddNodesAsync(new[]
        {
            new WorkflowNode(Guid.NewGuid(), template.Id, 1, 1, "舊節點", WorkflowNodeType.Apply, null)
        });
        var handler = new UpdateWorkflowTemplateCommandHandler(repo);
        var cmd = new UpdateWorkflowTemplateCommand(
            template.Id, "更新後名稱", "更新描述",
            new[] { new NodeInput(1, "新節點", WorkflowNodeType.Apply, null) });

        var dto = await handler.Handle(cmd, CancellationToken.None);

        dto.Version.Should().Be(1);
        dto.Name.Should().Be("更新後名稱");
        dto.Nodes.Should().HaveCount(1);
        dto.Nodes[0].Name.Should().Be("新節點");
    }

    [Fact]
    public async Task Update_Published_Template_Should_Bump_Version_And_Leave_Old_Nodes()
    {
        var repo = new FakeWorkflowTemplateRepository();
        var template = repo.Seed("UPD-002", "已發行範本");
        await repo.AddNodesAsync(new[]
        {
            new WorkflowNode(Guid.NewGuid(), template.Id, 1, 1, "v1 節點", WorkflowNodeType.Apply, null)
        });
        template.Publish();
        var handler = new UpdateWorkflowTemplateCommandHandler(repo);
        var cmd = new UpdateWorkflowTemplateCommand(
            template.Id, "已發行範本 v2", null,
            new[] { new NodeInput(1, "v2 節點", WorkflowNodeType.Process, null) });

        var dto = await handler.Handle(cmd, CancellationToken.None);

        dto.Version.Should().Be(2);
        dto.PublishedAt.Should().BeNull();
        dto.Nodes.Should().HaveCount(1);
        dto.Nodes[0].Name.Should().Be("v2 節點");

        // 舊版本節點應保留（版本隔離）
        var v1Nodes = await repo.ListNodesAsync(template.Id, 1, CancellationToken.None);
        v1Nodes.Should().HaveCount(1);
        v1Nodes[0].Name.Should().Be("v1 節點");
    }

    [Fact]
    public async Task Update_Nonexistent_Template_Should_Throw_DomainException()
    {
        var repo = new FakeWorkflowTemplateRepository();
        var handler = new UpdateWorkflowTemplateCommandHandler(repo);
        var cmd = new UpdateWorkflowTemplateCommand(Guid.NewGuid(), "名稱", null, Array.Empty<NodeInput>());

        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("WORKFLOW_TEMPLATE/NOT_FOUND");
    }
}
