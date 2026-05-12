using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Common;

namespace IsoDocs.Application.Cases.Commands;

public sealed record ReopenCaseCommand(
    Guid OriginalCaseId,
    string NewTitle,
    Guid RequestedByUserId) : ICommand<ReopenCaseResult>;

public sealed record ReopenCaseResult(Guid NewCaseId, Guid CaseRelationId);

public sealed class ReopenCaseCommandValidator : AbstractValidator<ReopenCaseCommand>
{
    public ReopenCaseCommandValidator()
    {
        RuleFor(x => x.OriginalCaseId).NotEmpty().WithMessage("OriginalCaseId 必填");
        RuleFor(x => x.NewTitle)
            .NotEmpty().WithMessage("新案標題必填")
            .MaximumLength(500).WithMessage("標題不可超過 500 字");
        RuleFor(x => x.RequestedByUserId).NotEmpty().WithMessage("RequestedByUserId 必填");
    }
}

public sealed class ReopenCaseCommandHandler : ICommandHandler<ReopenCaseCommand, ReopenCaseResult>
{
    private readonly ICaseRepository _cases;
    private readonly ICaseRelationRepository _caseRelations;

    public ReopenCaseCommandHandler(ICaseRepository cases, ICaseRelationRepository caseRelations)
    {
        _cases = cases;
        _caseRelations = caseRelations;
    }

    public async Task<ReopenCaseResult> Handle(ReopenCaseCommand request, CancellationToken cancellationToken)
    {
        var original = await _cases.FindByIdAsync(request.OriginalCaseId, cancellationToken);
        if (original is null)
            throw new DomainException("case.not_found", $"找不到案件 {request.OriginalCaseId}");

        if (original.Status != CaseStatus.Closed)
            throw new DomainException("case.not_closed", "只有已結案的案件可重開新案");

        var newCaseId = Guid.NewGuid();
        // 真實流水號由 issue #16 的 DocumentType 編碼服務提供；此處以暫行格式取代
        var newCaseNumber = $"REOPEN-{newCaseId:N}";
        var newCase = new Case(
            id: newCaseId,
            caseNumber: newCaseNumber,
            title: request.NewTitle,
            documentTypeId: original.DocumentTypeId,
            workflowTemplateId: original.WorkflowTemplateId,
            templateVersion: original.TemplateVersion,
            fieldVersion: original.FieldVersion,
            initiatedByUserId: request.RequestedByUserId,
            expectedCompletionAt: null,
            customerId: original.CustomerId);

        var relation = new CaseRelation(
            id: Guid.NewGuid(),
            parentCaseId: original.Id,
            childCaseId: newCase.Id,
            relationType: CaseRelationType.Reopen,
            createdByUserId: request.RequestedByUserId);

        await _cases.AddAsync(newCase, cancellationToken);
        await _caseRelations.AddAsync(relation, cancellationToken);
        await _cases.SaveChangesAsync(cancellationToken);

        return new ReopenCaseResult(newCase.Id, relation.Id);
    }
}
