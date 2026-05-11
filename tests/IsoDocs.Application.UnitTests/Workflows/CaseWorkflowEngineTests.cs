using FluentAssertions;
using IsoDocs.Application.Workflows;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Common;
using Xunit;

namespace IsoDocs.Application.UnitTests.Workflows;

public class CaseWorkflowEngineTests
{
    private readonly CaseWorkflowEngine _engine = new();

    private static Case CreateCase() =>
        new(Guid.NewGuid(), "ITCT-F01-260001", "測試案件",
            Guid.NewGuid(), Guid.NewGuid(), 1, 1, Guid.NewGuid(), null, null);

    private static CaseNode CreateNode(Guid caseId, int nodeOrder,
        CaseNodeStatus status = CaseNodeStatus.Pending)
    {
        var node = new CaseNode(Guid.NewGuid(), caseId, Guid.NewGuid(),
            nodeOrder, $"節點{nodeOrder}", null, null);
        if (status == CaseNodeStatus.InProgress)
            node.Accept(Guid.NewGuid());
        else if (status == CaseNodeStatus.Completed)
        {
            node.Accept(Guid.NewGuid());
            node.Complete();
        }
        return node;
    }

    [Fact]
    public void AdvanceAfterNodeComplete_LastNode_ClosesCase()
    {
        var @case = CreateCase();
        var node1 = CreateNode(@case.Id, 1, CaseNodeStatus.Completed);

        var result = _engine.AdvanceAfterNodeComplete(@case, [node1], node1.Id);

        result.Type.Should().Be(WorkflowTransitionType.Closed);
        @case.Status.Should().Be(CaseStatus.Closed);
    }

    [Fact]
    public void AdvanceAfterNodeComplete_WithNextNode_ActivatesNextNode()
    {
        var @case = CreateCase();
        var node1 = CreateNode(@case.Id, 1, CaseNodeStatus.Completed);
        var node2 = CreateNode(@case.Id, 2);

        var result = _engine.AdvanceAfterNodeComplete(@case, [node1, node2], node1.Id);

        result.Type.Should().Be(WorkflowTransitionType.Advanced);
        result.ActivatedNodeIds.Should().Contain(node2.Id);
        @case.Status.Should().Be(CaseStatus.InProgress);
    }

    [Fact]
    public void AdvanceAfterNodeComplete_ParallelNodes_WaitsForAll()
    {
        var @case = CreateCase();
        var nodeA = CreateNode(@case.Id, 1, CaseNodeStatus.Completed);
        var nodeB = CreateNode(@case.Id, 1, CaseNodeStatus.InProgress);
        var node2 = CreateNode(@case.Id, 2);

        var result = _engine.AdvanceAfterNodeComplete(@case, [nodeA, nodeB, node2], nodeA.Id);

        result.Type.Should().Be(WorkflowTransitionType.Advanced);
        result.ActivatedNodeIds.Should().BeEmpty();
        @case.Status.Should().Be(CaseStatus.InProgress);
    }

    [Fact]
    public void AdvanceAfterNodeComplete_AllParallelComplete_ActivatesNext()
    {
        var @case = CreateCase();
        var nodeA = CreateNode(@case.Id, 1, CaseNodeStatus.Completed);
        var nodeB = CreateNode(@case.Id, 1, CaseNodeStatus.Completed);
        var node2 = CreateNode(@case.Id, 2);

        var result = _engine.AdvanceAfterNodeComplete(@case, [nodeA, nodeB, node2], nodeB.Id);

        result.Type.Should().Be(WorkflowTransitionType.Advanced);
        result.ActivatedNodeIds.Should().Contain(node2.Id);
    }

    [Fact]
    public void ReturnToPreviousNode_MarksRejectedAndReactivatesPrevious()
    {
        var @case = CreateCase();
        var node1 = CreateNode(@case.Id, 1, CaseNodeStatus.Completed);
        var node2 = CreateNode(@case.Id, 2, CaseNodeStatus.InProgress);

        var result = _engine.ReturnToPreviousNode(@case, [node1, node2], node2.Id);

        result.Type.Should().Be(WorkflowTransitionType.Returned);
        node2.Status.Should().Be(CaseNodeStatus.Returned);
        node1.Status.Should().Be(CaseNodeStatus.Pending);
        result.ReactivatedNodeIds.Should().Contain(node1.Id);
    }

    [Fact]
    public void ReturnToPreviousNode_FirstNode_NothingReactivated()
    {
        var @case = CreateCase();
        var node1 = CreateNode(@case.Id, 1, CaseNodeStatus.InProgress);

        var result = _engine.ReturnToPreviousNode(@case, [node1], node1.Id);

        result.Type.Should().Be(WorkflowTransitionType.Returned);
        result.ReactivatedNodeIds.Should().BeEmpty();
        node1.Status.Should().Be(CaseNodeStatus.Returned);
    }

    [Fact]
    public void VoidCase_SkipsAllNonCompletedNodes()
    {
        var @case = CreateCase();
        var node1 = CreateNode(@case.Id, 1, CaseNodeStatus.Completed);
        var node2 = CreateNode(@case.Id, 2, CaseNodeStatus.InProgress);
        var node3 = CreateNode(@case.Id, 3);

        _engine.VoidCase(@case, [node1, node2, node3]);

        @case.Status.Should().Be(CaseStatus.Voided);
        node1.Status.Should().Be(CaseNodeStatus.Completed);
        node2.Status.Should().Be(CaseNodeStatus.Skipped);
        node3.Status.Should().Be(CaseNodeStatus.Skipped);
    }

    [Fact]
    public void VoidCase_AlreadyVoided_ThrowsDomainException()
    {
        var @case = CreateCase();
        @case.Void();

        var act = () => _engine.VoidCase(@case, []);

        act.Should().Throw<DomainException>();
    }
}
