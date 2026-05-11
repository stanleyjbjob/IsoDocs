using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Common;
using MediatR;

namespace IsoDocs.Application.Cases.Commands;

/// <summary>
/// 退回至前一處理節點：將目前 InProgress 節點標為 Returned，並重新啟用前一個 Completed 節點。
/// 對應 POST /api/cases/{id}/actions/reject。
/// </summary>
public sealed record RejectCaseCommand(
    Guid CaseId,
    Guid ActorUserId,
    string? Comment) : ICommand;

public sealed class RejectCaseCommandValidator : AbstractValidator<RejectCaseCommand>
{
    public RejectCaseCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}

public sealed class RejectCaseCommandHandler : ICommandHandler<RejectCaseCommand>
{
    private readonly ICaseRepository _cases;
    private readonly IPublisher _publisher;

    public RejectCaseCommandHandler(ICaseRepository cases, IPublisher publisher)
    {
        _cases = cases;
        _publisher = publisher;
    }

    public async Task<Unit> Handle(RejectCaseCommand request, CancellationToken cancellationToken)
    {
        var @case = await _cases.FindByIdAsync(request.CaseId, cancellationToken)
            ?? throw new DomainException("case.not_found", $"案件 {request.CaseId} 不存在");

        if (@case.Status != CaseStatus.InProgress)
            throw new DomainException("case.invalid_status", "只有進行中的案件可退回");

        var nodes = await _cases.GetNodesAsync(request.CaseId, cancellationToken);
        var currentNode = nodes
            .Where(n => n.Status == CaseNodeStatus.InProgress)
            .OrderBy(n => n.NodeOrder)
            .FirstOrDefault()
            ?? throw new DomainException("case.no_active_node", "找不到進行中的節點");

        currentNode.Return();

        var previousNode = nodes
            .Where(n => n.NodeOrder < currentNode.NodeOrder && n.Status == CaseNodeStatus.Completed)
            .OrderByDescending(n => n.NodeOrder)
            .FirstOrDefault();

        previousNode?.Reactivate();

        var action = new CaseAction(
            Guid.NewGuid(), @case.Id, currentNode.Id,
            CaseActionType.Reject, request.ActorUserId, request.Comment, null);
        await _cases.AddActionAsync(action, cancellationToken);
        await _cases.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(
            new CaseActionNotification(@case.Id, currentNode.Id, CaseActionType.Reject, request.ActorUserId),
            cancellationToken);

        return Unit.Value;
    }
}
