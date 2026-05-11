using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Common;

namespace IsoDocs.Application.Cases.Commands;

/// <summary>
/// 衍生子流程：以父案件為基礎建立子案件，並建立 Subprocess 關聯紀錄。issue [5.3.1]。
/// </summary>
public sealed record SpawnChildCaseCommand(
    Guid ParentCaseId,
    string Title,
    Guid InitiatedByUserId) : ICommand<SpawnChildCaseResult>;

public sealed record SpawnChildCaseResult(
    Guid ChildCaseId,
    string CaseNumber,
    Guid RelationId);

public sealed class SpawnChildCaseCommandValidator : AbstractValidator<SpawnChildCaseCommand>
{
    public SpawnChildCaseCommandValidator()
    {
        RuleFor(x => x.ParentCaseId).NotEmpty();
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("子流程標題必填")
            .MaximumLength(300).WithMessage("子流程標題不可超過 300 字");
        RuleFor(x => x.InitiatedByUserId).NotEmpty();
    }
}

public sealed class SpawnChildCaseCommandHandler : ICommandHandler<SpawnChildCaseCommand, SpawnChildCaseResult>
{
    private readonly ICaseRepository _cases;
    private readonly ICaseRelationRepository _caseRelations;

    public SpawnChildCaseCommandHandler(ICaseRepository cases, ICaseRelationRepository caseRelations)
    {
        _cases = cases;
        _caseRelations = caseRelations;
    }

    public async Task<SpawnChildCaseResult> Handle(SpawnChildCaseCommand request, CancellationToken cancellationToken)
    {
        var parent = await _cases.FindByIdAsync(request.ParentCaseId, cancellationToken);
        if (parent is null)
            throw new DomainException("case.not_found", $"找不到案件 {request.ParentCaseId}");

        if (parent.Status != CaseStatus.InProgress)
            throw new DomainException("case.invalid_status", "只有進行中的案件可衍生子流程");

        // 子流程案號格式：{父案號}-C{8碼唯一碼}
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var childCaseNumber = $"{parent.CaseNumber}-C{suffix}";

        var childCase = new Case(
            id: Guid.NewGuid(),
            caseNumber: childCaseNumber,
            title: request.Title,
            documentTypeId: parent.DocumentTypeId,
            workflowTemplateId: parent.WorkflowTemplateId,
            templateVersion: parent.TemplateVersion,
            fieldVersion: parent.FieldVersion,
            initiatedByUserId: request.InitiatedByUserId,
            expectedCompletionAt: null,
            customerId: parent.CustomerId);

        var relation = new CaseRelation(
            id: Guid.NewGuid(),
            parentCaseId: parent.Id,
            childCaseId: childCase.Id,
            relationType: CaseRelationType.Subprocess,
            createdByUserId: request.InitiatedByUserId);

        await _cases.AddAsync(childCase, cancellationToken);
        await _caseRelations.AddAsync(relation, cancellationToken);
        // _cases 與 _caseRelations 共用同一 DbContext scope，一次 SaveChanges 即可
        await _caseRelations.SaveChangesAsync(cancellationToken);

        return new SpawnChildCaseResult(childCase.Id, childCaseNumber, relation.Id);
    }
}
