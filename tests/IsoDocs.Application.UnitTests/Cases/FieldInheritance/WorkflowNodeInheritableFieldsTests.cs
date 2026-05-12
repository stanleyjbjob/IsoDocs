using FluentAssertions;
using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.UnitTests.Cases.FieldInheritance;

public class WorkflowNodeInheritableFieldsTests
{
    private static WorkflowNode BuildNode(string configJson = "{}")
    {
        var node = new WorkflowNode(
            id: Guid.NewGuid(),
            workflowTemplateId: Guid.NewGuid(),
            templateVersion: 1,
            nodeOrder: 1,
            name: "發起",
            nodeType: WorkflowNodeType.Apply,
            requiredRoleId: null);

        if (configJson != "{}")
        {
            // 透過 SetInheritableFields 驗證初始設定
        }

        return node;
    }

    [Fact]
    public void GetInheritableFieldIds_Returns_Empty_For_Default_ConfigJson()
    {
        var node = BuildNode();
        node.GetInheritableFieldIds().Should().BeEmpty();
    }

    [Fact]
    public void SetInheritableFields_Stores_Ids_And_GetInheritableFieldIds_Returns_Them()
    {
        var node = BuildNode();
        var fieldIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        node.SetInheritableFields(fieldIds);

        node.GetInheritableFieldIds().Should().BeEquivalentTo(fieldIds);
    }

    [Fact]
    public void SetInheritableFields_With_Empty_List_Returns_Empty()
    {
        var node = BuildNode();
        node.SetInheritableFields(new[] { Guid.NewGuid() });
        node.SetInheritableFields(Array.Empty<Guid>());

        node.GetInheritableFieldIds().Should().BeEmpty();
    }

    [Fact]
    public void SetInheritableFields_Overwrites_Previous_Setting()
    {
        var node = BuildNode();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();

        node.SetInheritableFields(new[] { firstId });
        node.SetInheritableFields(new[] { secondId });

        node.GetInheritableFieldIds().Should().ContainSingle()
            .Which.Should().Be(secondId);
    }

    [Fact]
    public void SetInheritableFields_Updates_UpdatedAt()
    {
        var node = BuildNode();
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        node.SetInheritableFields(new[] { Guid.NewGuid() });

        node.UpdatedAt.Should().NotBeNull();
        node.UpdatedAt.Should().BeAfter(before);
    }
}
