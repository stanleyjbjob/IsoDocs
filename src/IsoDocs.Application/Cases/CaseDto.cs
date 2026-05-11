namespace IsoDocs.Application.Cases;

public sealed record CaseDto(
    Guid Id,
    string CaseNumber,
    string Title,
    Guid DocumentTypeId,
    Guid WorkflowTemplateId,
    int TemplateVersion,
    string Status,
    Guid InitiatedByUserId,
    DateTimeOffset InitiatedAt,
    DateTimeOffset? ExpectedCompletionAt,
    DateTimeOffset? OriginalExpectedAt,
    Guid? CustomerId);
