namespace IsoDocs.Infrastructure.Blob;

public sealed class BlobStorageOptions
{
    public const string SectionName = "AzureBlob";

    /// <summary>Azure Blob Storage 連線字串（含 AccountKey）。於 user-secrets 或環境變數中提供。</summary>
    public string ConnectionString { get; init; } = string.Empty;

    /// <summary>附件儲存容器名稱。</summary>
    public string ContainerName { get; init; } = "attachments";
}
