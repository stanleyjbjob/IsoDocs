using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using IsoDocs.Application.Attachments;
using Microsoft.Extensions.Options;

namespace IsoDocs.Infrastructure.Blob;

/// <summary>
/// Azure Blob Storage 服務實作。
/// 使用 StorageSharedKeyCredential（連線字串內含）產生 SAS URL。
/// </summary>
internal sealed class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _container;
    private readonly string _blobBaseUrl;

    public BlobStorageService(IOptions<BlobStorageOptions> options)
    {
        var opts = options.Value;
        _container = new BlobContainerClient(opts.ConnectionString, opts.ContainerName);
        _blobBaseUrl = _container.Uri.AbsoluteUri.TrimEnd('/');
    }

    public Task<BlobUploadToken> GenerateUploadTokenAsync(
        string blobPath, string contentType, CancellationToken cancellationToken = default)
    {
        var blobClient = _container.GetBlobClient(blobPath);
        var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

        var sas = new BlobSasBuilder(BlobSasPermissions.Write | BlobSasPermissions.Create, expiresOn)
        {
            BlobContainerName = _container.Name,
            BlobName = blobPath,
            Resource = "b",
            ContentType = contentType
        };

        var uri = blobClient.GenerateSasUri(sas);
        return Task.FromResult(new BlobUploadToken(uri.ToString(), expiresOn));
    }

    public Task<BlobDownloadToken> GenerateDownloadTokenAsync(
        string blobPath, CancellationToken cancellationToken = default)
    {
        var blobClient = _container.GetBlobClient(blobPath);
        var expiresOn = DateTimeOffset.UtcNow.AddMinutes(30);

        var sas = new BlobSasBuilder(BlobSasPermissions.Read, expiresOn)
        {
            BlobContainerName = _container.Name,
            BlobName = blobPath,
            Resource = "b"
        };

        var uri = blobClient.GenerateSasUri(sas);
        return Task.FromResult(new BlobDownloadToken(uri.ToString(), expiresOn));
    }

    public string BuildBlobUrl(string blobPath) => $"{_blobBaseUrl}/{blobPath}";
}
