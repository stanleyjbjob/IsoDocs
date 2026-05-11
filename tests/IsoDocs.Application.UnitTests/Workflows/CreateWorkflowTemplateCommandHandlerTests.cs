using FluentAssertions;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Application.Workflows.Commands;
using IsoDocs.Domain.Common;
using IsoDocs.Domain.Workflows;
using Xunit;

namespace IsoDocs.Application.UnitTests.Workflows;

public class CreateWorkflowTemplateCommandHandlerTests
{
    [Fact]
    public async Task Create_With_Valid_Input_Should_Persist_Template_And_Return_Dto()
    {
        var repo = new FakeWorkflowTemplateRepository();
        var handler = new CreateWorkflowTemplateCommandHandler(repo);
        var userId = Guid.NewGuid();
        var nodes = new[]
        {
            new NodeInput(1, "申請", WorkflowNodeType.Apply, null),
            new NodeInput(2, "處理", WorkflowNodeType.Process, null)
        };
        var cmd = new CreateWorkflowTemplateCommand("TMPL-001", "測試範本", "測試用", userId, nodes);

        var dto = await handler.Handle(cmd, CancellationToken.None);

        dto.Should().NotBeNull();
        dto.Code.Should().Be("TMPL-001");
        dto.Name.Should().Be("測試範本");
        dto.Description.Should().Be("測試用");
        dto.Version.Should().Be(1);
        dto.PublishedAt.Should().BeNull();
        dto.Nodes.Should().HaveCount(2);
        dto.Nodes[0].Name.Should().Be("申請");
        dto.Nodes[1].Name.Should().Be("處理");
    }

    [Fact]
    public async Task Create_With_Duplicate_Code_Should_Throw_DomainException()
    {
        var repo = new FakeWorkflowTemplateRepository();
        repo.Seed("TMPL-001", "已存在範本");
        var handler = new CreateWorkflowTemplateCommandHandler(repo);
        var cmd = new CreateWorkflowTemplateCommand("TMPL-001", "新範本", null, Guid.NewGuid(), Array.Empty<NodeInput>());

        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("WORKFLOW_TEMPLATE/CODE_DUPLICATE");
    }

    [Fact]
    public async Task Create_Without_Nodes_Should_Succeed_With_Empty_Nodes()
    {
        var repo = new FakeWorkflowTemplateRepository();
        var handler = new CreateWorkflowTemplateCommandHandler(repo);
        var cmd = new CreateWorkflowTemplateCommand("TMPL-002", "空節點範本", null, Guid.NewGuid(), Array.Empty<NodeInput>());

        var dto = await handler.Handle(cmd, CancellationToken.None);

        dto.Nodes.Should().BeEmpty();
    }
}
