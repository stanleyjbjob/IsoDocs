using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Common;
using MediatR;

namespace IsoDocs.Application.Cases.Commands;

/// <summary>
/// 回覆結案：完成目前 InProgress 節點；若無下一節點則自動結案。
/// 對應 POST /api/cases/{id}/actions/reply-close。
/// </summary>
public sealed record ReplyCaseCommand(
    Guid CaseId,
    Guid ActorUserId,
    string? Comment) : ICommand;

public sealed class ReplyCaseCommandValidator : AbstractValidator<ReplyCaseCommand>
{
    public ReplyCaseCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}

public sealed class ReplyCaseCommandHandler : ICommandHandler<ReplyCaseCommand>
{
    private readonly ICaseRepository _cases;
    private readonly IPublisher _publisher;

    public ReplyCaseCommandHandler(ICaseRepository cases, IPublisher publisher)
    {
        _cases = cases;
        _publisher = publisher;
    }

    public async Task<Unit> Handle(ReplyCaseCommand request, CancellationToken cancellationToken)
    {
        var @case = await _cases.FindByIdAsync(request.CaseId, cancellationToken)
            ?? throw new DomainException("case.not_found", $"案件 {request.CaseId} 不存在");

        if (@case.Status != CaseStatus.InProgress)
            throw new DomainException("case.invalid_status", "只有進行中的案件可回覆結案");

        var nodes = await _cases.GetNodesAsync(request.CaseId, cancellationToken);
        var currentNode = nodes
            .Where(n => n.Status == CaseNodeStatus.InProgress)
            .OrderBy(n => n.NodeOrder)
            .FirstOrDefault()
            ?? throw new DomainException("case.no_active_node", "找不到進行中的節點");

        currentNode.Complete();

        var hasNextNode = nodes.Any(n =>
            n.NodeOrder > currentNode.NodeOrder && n.Status == CaseNodeStatus.Pending);

        if (!hasNextNode)
            @case.Close();

        var action = new CaseAction(
            Guid.NewGuid(), @case.Id, currentNode.Id,
            CaseActionType.ReplyClose, request.ActorUserId, request.Comment, null);
        await _cases.AddActionAsync(action, cancellationToken);
        await _cases.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(
            new CaseActionNotification(@case.Id, currentNode.Id, CaseActionType.ReplyClose, request.ActorUserId),
            cancellationToken);

        return Unit.Value;
    }
}
