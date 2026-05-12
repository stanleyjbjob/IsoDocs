using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.Cases.Queries;

/// <summary>
/// 案件清單摘要 DTO（供 GET /api/cases 與 GET /api/cases/search 使用）。
/// </summary>
public sealed record CaseSummaryDto(
    Guid Id,
    string CaseNumber,
    string Title,
    CaseStatus Status,
    Guid DocumentTypeId,
    string? DocumentTypeName,
    Guid? CustomerId,
    string? CustomerName,
    DateTimeOffset InitiatedAt,
    DateTimeOffset? ExpectedCompletionAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset? VoidedAt,
    string? CustomVersionNumber);
