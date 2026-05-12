using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Common;
using MediatR;

namespace IsoDocs.Application.Cases.Commands;

/// <summary>
/// 管理者手動指派案件節點的頂替人員。對應 POST /api/admin/cases/{caseId}/reassign。
/// 用於未設定代理時由管理者手動頂替（issue #30 [2.3.2]）。
/// </summary>
public sealed record ReassignCaseNodeCommand(
    Guid CaseId,
    Guid NodeId,
    Guid NewAssigneeUserId) : ICommand;

public sealed class ReassignCaseNodeCommandValidator : AbstractValidator<ReassignCaseNodeCommand>
{
    public ReassignCaseNodeCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.NodeId).NotEmpty();
        RuleFor(x => x.NewAssigneeUserId).NotEmpty();
    }
}

public sealed class ReassignCaseNodeCommandHandler : ICommandHandler<ReassignCaseNodeCommand>
{
    private readonly ICaseNodeRepository _caseNodes;

    public ReassignCaseNodeCommandHandler(ICaseNodeRepository caseNodes)
    {
        _caseNodes = caseNodes;
    }

    public async Task<Unit> Handle(ReassignCaseNodeCommand request, CancellationToken cancellationToken)
    {
        var node = await _caseNodes.GetByCaseAndNodeAsync(request.CaseId, request.NodeId, cancellationToken);
        if (node is null)
            throw new DomainException("case_node.not_found", $"找不到案件 {request.CaseId} 的節點 {request.NodeId}");

        if (node.Status == CaseNodeStatus.Completed || node.Status == CaseNodeStatus.Skipped)
            throw new DomainException("case_node.already_finished", "已完成或跳過的節點無法重新指派");

        node.Reassign(request.NewAssigneeUserId);
        await _caseNodes.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
