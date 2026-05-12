using FluentValidation;
using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Common;

namespace IsoDocs.Application.Cases.FieldInheritance;

/// <summary>
/// 子流程建立後，從主單繼承指定欄位值（issue [5.3.2]）。
/// 回傳已繼承的欄位 DTO 清單，前端可根據 <see cref="CaseFieldInheritanceDto.InheritedFromCaseId"/>
/// 顯示繼承來源標示。
/// </summary>
public sealed record InheritFieldsFromParentCommand(
    Guid ChildCaseId,
    Guid ParentCaseId,
    IReadOnlyList<Guid> InheritableFieldDefinitionIds) : ICommand<IReadOnlyList<CaseFieldInheritanceDto>>;

public sealed class InheritFieldsFromParentCommandValidator : AbstractValidator<InheritFieldsFromParentCommand>
{
    public InheritFieldsFromParentCommandValidator()
    {
        RuleFor(x => x.ChildCaseId).NotEmpty().WithMessage("子流程案件 ID 必填");
        RuleFor(x => x.ParentCaseId).NotEmpty().WithMessage("主單案件 ID 必填");
        RuleFor(x => x.ChildCaseId)
            .NotEqual(x => x.ParentCaseId)
            .WithMessage("子流程與主單不可為同一案件");
        RuleFor(x => x.InheritableFieldDefinitionIds)
            .NotNull().WithMessage("可繼承欄位清單不可為 null（空陣列代表不繼承任何欄位）");
    }
}

public sealed class InheritFieldsFromParentCommandHandler
    : ICommandHandler<InheritFieldsFromParentCommand, IReadOnlyList<CaseFieldInheritanceDto>>
{
    private readonly ICaseFieldRepository _caseFields;

    public InheritFieldsFromParentCommandHandler(ICaseFieldRepository caseFields)
    {
        _caseFields = caseFields;
    }

    public async Task<IReadOnlyList<CaseFieldInheritanceDto>> Handle(
        InheritFieldsFromParentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.InheritableFieldDefinitionIds.Count == 0)
            return Array.Empty<CaseFieldInheritanceDto>();

        var parentFields = await _caseFields.ListByCaseIdAsync(request.ParentCaseId, cancellationToken);

        var inheritableSet = new HashSet<Guid>(request.InheritableFieldDefinitionIds);
        var toInherit = parentFields
            .Where(f => inheritableSet.Contains(f.FieldDefinitionId))
            .ToList();

        if (toInherit.Count == 0)
            return Array.Empty<CaseFieldInheritanceDto>();

        var childFields = toInherit
            .Select(pf => CaseField.CreateInherited(
                id: Guid.NewGuid(),
                caseId: request.ChildCaseId,
                fieldDefinitionId: pf.FieldDefinitionId,
                fieldVersion: pf.FieldVersion,
                fieldCode: pf.FieldCode,
                valueJson: pf.ValueJson,
                inheritedFromCaseId: request.ParentCaseId))
            .ToList();

        await _caseFields.AddRangeAsync(childFields, cancellationToken);
        await _caseFields.SaveChangesAsync(cancellationToken);

        return childFields
            .Select(CaseFieldInheritanceDto.FromCaseField)
            .ToList();
    }
}

/// <summary>
/// 繼承欄位結果 DTO。前端根據 <see cref="InheritedFromCaseId"/> 顯示「繼承自主單」標示。
/// </summary>
public sealed record CaseFieldInheritanceDto(
    Guid Id,
    Guid CaseId,
    Guid FieldDefinitionId,
    string FieldCode,
    string ValueJson,
    Guid InheritedFromCaseId)
{
    internal static CaseFieldInheritanceDto FromCaseField(CaseField f) =>
        new(f.Id, f.CaseId, f.FieldDefinitionId, f.FieldCode, f.ValueJson, f.InheritedFromCaseId!.Value);
}
