using IsoDocs.Application.Attachments;

namespace IsoDocs.Api.IntegrationTests.Fakes;

/// <summary>
/// 整合測試用假 Blob 服務。回傳固定格式的假 SAS URL，不呼叫 Azure。
/// </summary>
public sealed class FakeBlobStorageService : IBlobStorageService
{
    public Task<BlobUploadToken> GenerateUploadTokenAsync(
        string blobPath, string contentType, CancellationToken cancellationToken = default)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var url = $"https://fake.blob.core.windows.net/attachments/{blobPath}?sv=fake&sp=cw&sig=fake";
        return Task.FromResult(new BlobUploadToken(url, expiresAt));
    }

    public Task<BlobDownloadToken> GenerateDownloadTokenAsync(
        string blobPath, CancellationToken cancellationToken = default)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
        var url = $"https://fake.blob.core.windows.net/attachments/{blobPath}?sv=fake&sp=r&sig=fake";
        return Task.FromResult(new BlobDownloadToken(url, expiresAt));
    }

    public string BuildBlobUrl(string blobPath) =>
        $"https://fake.blob.core.windows.net/attachments/{blobPath}";
}
