namespace FileBrowser.Infrastructure.FileSystem;

public sealed class AzureBlobOptions
{
    public const string SectionName = "FileBrowser:AzureBlob";

    /// <summary>
    /// Connection string for the storage account. Prefer <see cref="AccountUrl"/>
    /// with managed identity for anything deployed to Azure.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Blob service endpoint (e.g. https://myaccount.blob.core.windows.net).
    /// Used with DefaultAzureCredential when no connection string is set.
    /// </summary>
    public string? AccountUrl { get; set; }

    public string ContainerName { get; set; } = "files";
}
