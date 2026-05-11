using IsoDocs.Domain.Common;

namespace IsoDocs.Domain.Attachments;

/// <summary>
/// 附件。檔案本體儲存於 Azure Blob，本表只保留 metadata 與 BlobUrl。
/// 依需求設計：附件不因案件結案或作廢而專除（issue [7.2]）。
/// </summary>
public class Attachment : Entity<Guid>, IAggregateRoot
{
    public Guid CaseId { get; protected set; }
    public string FileName { get; protected set; } = string.Empty;
    public string ContentType { get; protected set; } = string.Empty;
    public long SizeBytes { get; protected set; }
    public string BlobUrl { get; protected set; } = string.Empty;
    public Guid UploadedByUserId { get; protected set; }
    public DateTimeOffset UploadedAt { get; protected set; } = DateTimeOffset.UtcNow;
    /// <summary>軟刪除標記，附件本體仍保留於 Blob。</summary>
    public bool IsDeleted { get; protected set; }

    private Attachment() { }

    public Attachment(Guid id, Guid caseId, string fileName, string contentType, long sizeBytes, string blobUrl, Guid uploadedByUserId)
    {
        Id = id;
        CaseId = caseId;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        BlobUrl = blobUrl;
        UploadedByUserId = uploadedByUserId;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
