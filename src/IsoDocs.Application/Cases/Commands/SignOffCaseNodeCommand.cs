using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Common;

namespace IsoDocs.Application.Cases.Commands;

/// <summary>
/// 對案件核准節點提交文件發行簽核。
/// 簽核意見寫入 CaseAction.Comment，簽核時間由 CaseAction 建構子記錄（DateTimeOffset.UtcNow）。
/// 對應 POST /api/cases/{caseId}/actions/sign-off。
/// </summary>
public sealed record SignOffCaseNodeCommand(
    Guid CaseId,
    Guid CaseNodeId,
    Guid ActorUserId,
    string? Comment) : ICommand<SignOffEntryDto>;

public sealed class SignOffCaseNodeCommandValidator : AbstractValidator<SignOffCaseNodeCommand>
{
    public SignOffCaseNodeCommandValidator()
    {
        RuleFor(x => x.CaseId).NotEmpty();
        RuleFor(x => x.CaseNodeId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.Comment)
            .MaximumLength(2000).WithMessage("簽核意見不可超過 2000 字");
    }
}

public sealed class SignOffCaseNodeCommandHandler : ICommandHandler<SignOffCaseNodeCommand, SignOffEntryDto>
{
    private readonly ICaseRepository _cases;

    public SignOffCaseNodeCommandHandler(ICaseRepository cases)
    {
        _cases = cases;
    }

    public async Task<SignOffEntryDto> Handle(SignOffCaseNodeCommand request, CancellationToken cancellationToken)
    {
        var @case = await _cases.FindByIdAsync(request.CaseId, cancellationToken);
        if (@case is null)
            throw new DomainException("case.not_found", $"案件 {request.CaseId} 不存在");

        if (@case.Status == CaseStatus.Voided)
            throw new DomainException("case.voided", "已作廢的案件不可簽核");

        var node = await _cases.FindNodeByIdAsync(request.CaseNodeId, cancellationToken);
        if (node is null || node.CaseId != request.CaseId)
            throw new DomainException("case_node.not_found", $"節點 {request.CaseNodeId} 不屬於此案件");

        var action = new CaseAction(
            id: Guid.NewGuid(),
            caseId: request.CaseId,
            caseNodeId: request.CaseNodeId,
            actionType: CaseActionType.SignOff,
            actorUserId: request.ActorUserId,
            comment: request.Comment,
            payloadJson: null);

        await _cases.AddActionAsync(action, cancellationToken);
        await _cases.SaveChangesAsync(cancellationToken);

        return new SignOffEntryDto(
            Id: action.Id,
            CaseId: action.CaseId,
            CaseNodeId: action.CaseNodeId,
            NodeName: node.NodeName,
            ActorUserId: action.ActorUserId,
            Comment: action.Comment,
            ActionAt: action.ActionAt);
    }
}
