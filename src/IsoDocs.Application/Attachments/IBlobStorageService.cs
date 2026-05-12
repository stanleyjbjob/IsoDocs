namespace IsoDocs.Application.Attachments;

public sealed record BlobUploadToken(string UploadUrl, DateTimeOffset ExpiresAt);
public sealed record BlobDownloadToken(string DownloadUrl, DateTimeOffset ExpiresAt);

/// <summary>
/// Azure Blob Storage 抽象。產生上傳/下載 SAS URL。
/// Infrastructure 層以 Azure.Storage.Blobs 實作；測試以 Fake 替換。
/// </summary>
public interface IBlobStorageService
{
    /// <summary>為指定 blobPath 產生 SAS 上傳 URL（1 小時效期）。</summary>
    Task<BlobUploadToken> GenerateUploadTokenAsync(string blobPath, string contentType, CancellationToken cancellationToken = default);

    /// <summary>為指定 blobPath 產生 SAS 下載 URL（30 分鐘效期）。</summary>
    Task<BlobDownloadToken> GenerateDownloadTokenAsync(string blobPath, CancellationToken cancellationToken = default);

    /// <summary>依 blobPath 建立永久性 Blob URL（不含 SAS）。</summary>
    string BuildBlobUrl(string blobPath);
}
