namespace IsoDocs.Application.Cases;

public sealed record TodoItemDto(
    Guid CaseNodeId,
    Guid CaseId,
    string CaseNumber,
    string CaseTitle,
    string NodeName,
    int NodeOrder,
    string Status,
    DateTimeOffset? ExpectedAt);
