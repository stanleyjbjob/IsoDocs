namespace IsoDocs.Application.Attachments;

public sealed record AttachmentDto(
    Guid Id,
    Guid CaseId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid UploadedByUserId,
    DateTimeOffset UploadedAt);

/// <summary>
/// 上傳發起回應。包含 Azure Blob SAS 上傳 URL，前端直接對此 URL 進行 PUT 上傳。
/// </summary>
public sealed record UploadInitiationDto(
    Guid AttachmentId,
    string UploadUrl,
    DateTimeOffset ExpiresAt);

/// <summary>
/// 下載 URL 回應。SAS URL 具時效（30 分鐘），前端用此 URL 直接向 Azure Blob 下載。
/// </summary>
public sealed record AttachmentDownloadDto(
    Guid AttachmentId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string DownloadUrl,
    DateTimeOffset ExpiresAt,
    DateTimeOffset UploadedAt);
