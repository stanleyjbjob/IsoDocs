namespace IsoDocs.Application.Cases;

public sealed record CaseSummaryDto(
    Guid Id,
    string CaseNumber,
    string Title,
    string Status,
    DateTimeOffset InitiatedAt,
    DateTimeOffset? ExpectedCompletionAt,
    string? AssigneeDisplayName);
