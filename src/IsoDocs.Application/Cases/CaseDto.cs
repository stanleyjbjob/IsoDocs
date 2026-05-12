namespace IsoDocs.Application.Cases;

/// <summary>
/// 案件摘要 DTO，用於 PUT /api/cases/{id}/expected-completion 回應。
/// </summary>
public sealed record CaseDto(
    Guid Id,
    string CaseNumber,
    string Title,
    string Status,
    DateTimeOffset? ExpectedCompletionAt,
    DateTimeOffset? OriginalExpectedAt);
