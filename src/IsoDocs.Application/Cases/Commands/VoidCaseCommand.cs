using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Common;

namespace IsoDocs.Application.Cases.Commands;

/// <summary>
/// 作廢案件。
/// - 主單作廢：自動連鎖作廢所有狀態非 Voided/Closed 的子流程，寫入 VoidCascade 軌跡。
/// - 子流程獨立作廢：僅作廢本身，不影響主單。
/// 對應 POST /api/cases/{id}/actions/void，需 cases.void 權限。issue [5.3.3]。
/// </summary>
public sealed record VoidCaseCommand(
    Guid CaseId,
    Guid ActorUserId,
    string? Comment) : ICommand;

public sealed class VoidCaseCommandValidator : AbstractValidator<VoidCaseCommand>
{
    public VoidCaseCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty().WithMessage("CaseId 必填");
        RuleFor(x => x.ActorUserId).NotEmpty().WithMessage("ActorUserId 必填");
        RuleFor(x => x.Comment).MaximumLength(1000).WithMessage("備註不可超過 1000 字");
    }
}

public sealed class VoidCaseCommandHandler : ICommandHandler<VoidCaseCommand>
{
    private readonly ICaseRepository _cases;

    public VoidCaseCommandHandler(ICaseRepository cases)
    {
        _cases = cases;
    }

    public async Task<MediatR.Unit> Handle(VoidCaseCommand request, CancellationToken cancellationToken)
    {
        var @case = await _cases.FindByIdAsync(request.CaseId, cancellationToken)
            ?? throw new DomainException("case.not_found", $"找不到案件 {request.CaseId}");

        @case.Void();

        var voidAction = new CaseAction(
            id: Guid.NewGuid(),
            caseId: @case.Id,
            caseNodeId: null,
            actionType: CaseActionType.Void,
            actorUserId: request.ActorUserId,
            comment: request.Comment,
            payloadJson: null);
        await _cases.AddCaseActionAsync(voidAction, cancellationToken);

        // 連鎖作廢所有未完成的子流程（僅限 Subprocess 類型關聯的子案件）
        var children = await _cases.FindSubprocessChildrenAsync(@case.Id, cancellationToken);
        foreach (var child in children)
        {
            if (child.Status == CaseStatus.Voided || child.Status == CaseStatus.Closed)
                continue;

            child.Void();

            var cascadeAction = new CaseAction(
                id: Guid.NewGuid(),
                caseId: child.Id,
                caseNodeId: null,
                actionType: CaseActionType.VoidCascade,
                actorUserId: request.ActorUserId,
                comment: $"因主單 {request.CaseId} 作廢而連鎖作廢",
                payloadJson: null);
            await _cases.AddCaseActionAsync(cascadeAction, cancellationToken);
        }

        // TODO: issue #24 — 觸發主單作廢與子流程獨立作廢通知事件

        await _cases.SaveChangesAsync(cancellationToken);

        return MediatR.Unit.Value;
    }
}
