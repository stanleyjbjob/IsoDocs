namespace IsoDocs.Application.Cases;

public sealed record CaseDto(
    Guid Id,
    string CaseNumber,
    string Title,
    string Status,
    string? CustomVersionNumber,
    DateTimeOffset InitiatedAt,
    DateTimeOffset? ExpectedCompletionAt,
    DateTimeOffset? OriginalExpectedAt);
