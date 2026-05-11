using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Common;
using MediatR;

namespace IsoDocs.Application.Cases.Commands;

/// <summary>
/// 接單：將目前最靠前的 Pending 節點轉為 InProgress，並寫入 CaseAction 軌跡。
/// 對應 POST /api/cases/{id}/actions/accept。
/// </summary>
public sealed record AcceptCaseCommand(
    Guid CaseId,
    Guid ActorUserId,
    string? Comment) : ICommand;

public sealed class AcceptCaseCommandValidator : AbstractValidator<AcceptCaseCommand>
{
    public AcceptCaseCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}

public sealed class AcceptCaseCommandHandler : ICommandHandler<AcceptCaseCommand>
{
    private readonly ICaseRepository _cases;
    private readonly IPublisher _publisher;

    public AcceptCaseCommandHandler(ICaseRepository cases, IPublisher publisher)
    {
        _cases = cases;
        _publisher = publisher;
    }

    public async Task<Unit> Handle(AcceptCaseCommand request, CancellationToken cancellationToken)
    {
        var @case = await _cases.FindByIdAsync(request.CaseId, cancellationToken)
            ?? throw new DomainException("case.not_found", $"案件 {request.CaseId} 不存在");

        if (@case.Status != CaseStatus.InProgress)
            throw new DomainException("case.invalid_status", "只有進行中的案件可接單");

        var nodes = await _cases.GetNodesAsync(request.CaseId, cancellationToken);
        var currentNode = nodes
            .Where(n => n.Status == CaseNodeStatus.Pending)
            .OrderBy(n => n.NodeOrder)
            .FirstOrDefault()
            ?? throw new DomainException("case.no_pending_node", "找不到待處理的節點");

        currentNode.Accept(request.ActorUserId);

        var action = new CaseAction(
            Guid.NewGuid(), @case.Id, currentNode.Id,
            CaseActionType.Accept, request.ActorUserId, request.Comment, null);
        await _cases.AddActionAsync(action, cancellationToken);
        await _cases.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(
            new CaseActionNotification(@case.Id, currentNode.Id, CaseActionType.Accept, request.ActorUserId),
            cancellationToken);

        return Unit.Value;
    }
}
