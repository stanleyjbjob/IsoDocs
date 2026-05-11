using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.Cases;

internal static class CaseDtoMapper
{
    public static CaseDto ToDto(Case @case) => new(
        Id: @case.Id,
        CaseNumber: @case.CaseNumber,
        Title: @case.Title,
        DocumentTypeId: @case.DocumentTypeId,
        WorkflowTemplateId: @case.WorkflowTemplateId,
        TemplateVersion: @case.TemplateVersion,
        Status: @case.Status.ToString(),
        InitiatedByUserId: @case.InitiatedByUserId,
        InitiatedAt: @case.InitiatedAt,
        ExpectedCompletionAt: @case.ExpectedCompletionAt,
        OriginalExpectedAt: @case.OriginalExpectedAt,
        CustomerId: @case.CustomerId);
}
