using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Common;
using MediatR;

namespace IsoDocs.Application.Cases.Commands;

/// <summary>
/// 指派案件承辦人。對應 PUT /api/cases/{id}/assign 端點。
/// 將當前待處理節點指派給指定使用者並發布 CaseNodeAssignedNotification。
/// </summary>
public sealed record AssignCaseCommand(
    Guid CaseId,
    Guid AssigneeUserId) : ICommand;

public sealed class AssignCaseCommandValidator : AbstractValidator<AssignCaseCommand>
{
    public AssignCaseCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty().WithMessage("案件 Id 必填");
        RuleFor(x => x.AssigneeUserId).NotEmpty().WithMessage("承辦人 Id 必填");
    }
}

public sealed class AssignCaseCommandHandler : ICommandHandler<AssignCaseCommand>
{
    private readonly ICaseRepository _cases;
    private readonly IPublisher _publisher;

    public AssignCaseCommandHandler(ICaseRepository cases, IPublisher publisher)
    {
        _cases = cases;
        _publisher = publisher;
    }

    public async Task<MediatR.Unit> Handle(AssignCaseCommand request, CancellationToken cancellationToken)
    {
        var @case = await _cases.FindByIdAsync(request.CaseId, cancellationToken);
        if (@case is null)
            throw new DomainException("case.not_found", "案件不存在");

        if (@case.Status == CaseStatus.Voided)
            throw new DomainException("case.voided", "案件已作廢，無法指派");

        var activeNode = await _cases.FindActiveNodeAsync(request.CaseId, cancellationToken);
        if (activeNode is null)
            throw new DomainException("case.no_active_node", "案件無可指派的進行中節點");

        activeNode.Reassign(request.AssigneeUserId);
        await _cases.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(
            new CaseNodeAssignedNotification(request.CaseId, activeNode.Id, request.AssigneeUserId),
            cancellationToken);

        return MediatR.Unit.Value;
    }
}

/// <summary>
/// 節點承辦人指派通知。由 issue #24 [6.2] 實作的通知訂閱者處理。
/// </summary>
public sealed record CaseNodeAssignedNotification(
    Guid CaseId,
    Guid NodeId,
    Guid AssigneeUserId) : INotification;
