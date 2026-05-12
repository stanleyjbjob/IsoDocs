namespace IsoDocs.Application.Cases.Export;

public record CasePdfData(
    string CaseNumber,
    string Title,
    string Status,
    string InitiatedByUserName,
    DateTimeOffset InitiatedAt,
    DateTimeOffset? ExpectedCompletionAt,
    DateTimeOffset? OriginalExpectedAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset? VoidedAt,
    string? CustomVersionNumber,
    string? CustomerName,
    IReadOnlyList<CaseFieldPdfItem> Fields,
    IReadOnlyList<CaseNodePdfItem> Nodes,
    IReadOnlyList<CaseActionPdfItem> Actions,
    IReadOnlyList<CommentPdfItem> Comments,
    IReadOnlyList<AttachmentPdfItem> Attachments);

public record CaseFieldPdfItem(string FieldCode, string ValueJson);

public record CaseNodePdfItem(
    int NodeOrder,
    string NodeName,
    string Status,
    string? AssigneeUserName,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? ModifiedExpectedAt);

public record CaseActionPdfItem(
    string ActionType,
    string ActorUserName,
    string? Comment,
    DateTimeOffset ActionAt);

public record CommentPdfItem(
    string AuthorUserName,
    string Body,
    DateTimeOffset CreatedAt);

public record AttachmentPdfItem(
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedAt,
    string UploadedByUserName);
