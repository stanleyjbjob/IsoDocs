using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.Cases;

public sealed record CaseDto(
    Guid Id,
    string CaseNumber,
    string Title,
    string Status,
    Guid DocumentTypeId,
    Guid WorkflowTemplateId,
    Guid InitiatedByUserId,
    DateTimeOffset InitiatedAt,
    DateTimeOffset? ExpectedCompletionAt,
    DateTimeOffset? OriginalExpectedAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset? VoidedAt,
    string? CustomVersionNumber);

public static class CaseDtoMapper
{
    public static CaseDto ToDto(Case c) => new(
        Id: c.Id,
        CaseNumber: c.CaseNumber,
        Title: c.Title,
        Status: c.Status.ToString(),
        DocumentTypeId: c.DocumentTypeId,
        WorkflowTemplateId: c.WorkflowTemplateId,
        InitiatedByUserId: c.InitiatedByUserId,
        InitiatedAt: c.InitiatedAt,
        ExpectedCompletionAt: c.ExpectedCompletionAt,
        OriginalExpectedAt: c.OriginalExpectedAt,
        ClosedAt: c.ClosedAt,
        VoidedAt: c.VoidedAt,
        CustomVersionNumber: c.CustomVersionNumber);
}
