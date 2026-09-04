using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FileBrowser.Application.Abtractstions.FileSystem;

namespace FileBrowser.Infrastructure.FileSystem;

/// <summary>
/// Presents an Azure Blob Storage container as a virtual directory tree.
/// Blob storage is flat, so "directories" are inferred from "/" delimited
/// blob name prefixes rather than being real objects.
/// </summary>
public sealed class AzureBlobFileSystem : IFileSystem
{
    private const string Delimiter = "/";

    private readonly BlobContainerClient _containerClient;

    public AzureBlobFileSystem(BlobContainerClient containerClient)
    {
        _containerClient = containerClient
            ?? throw new ArgumentNullException(nameof(containerClient));
    }

    public async Task<DirectoryContents> GetDirectoryContentsAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var prefix = ToBlobPrefix(path);
        var entries = new List<FileItem>();

        await foreach (var item in _containerClient.GetBlobsByHierarchyAsync(
            prefix: prefix,
            delimiter: Delimiter,
            cancellationToken: cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();

            entries.Add(item.IsPrefix
                ? MapDirectory(item.Prefix)
                : MapFile(item.Blob));
        }

        return new DirectoryContents(ToVirtualPath(prefix), entries);
    }

    public async Task<FileItem?> GetFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var blobName = ToBlobName(path);

        if (string.IsNullOrEmpty(blobName))
        {
            return new FileItem("/", "/", FileSystemEntryType.Directory, null, DateTimeOffset.MinValue);
        }

        var blobClient = _containerClient.GetBlobClient(blobName);
        var properties = await TryGetPropertiesAsync(blobClient, cancellationToken);

        if (properties is not null)
        {
            return new FileItem(
                Name: GetName(blobName),
                Path: ToVirtualPath(blobName),
                Type: FileSystemEntryType.File,
                Size: properties.ContentLength,
                LastModified: properties.LastModified);
        }

        if (await HasAnyBlobWithPrefixAsync(blobName + Delimiter, cancellationToken))
        {
            return new FileItem(
                Name: GetName(blobName),
                Path: ToVirtualPath(blobName + Delimiter),
                Type: FileSystemEntryType.Directory,
                Size: null,
                LastModified: DateTimeOffset.MinValue);
        }

        return null;
    }

    public async Task<Stream> OpenReadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var blobName = ToBlobName(path);
        var blobClient = _containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            throw new FileNotFoundException(
                $"File '{path}' was not found.",
                path);
        }

        return await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var blobName = ToBlobName(path);

        if (string.IsNullOrEmpty(blobName))
        {
            return true;
        }

        var blobClient = _containerClient.GetBlobClient(blobName);

        if (await blobClient.ExistsAsync(cancellationToken))
        {
            return true;
        }

        return await HasAnyBlobWithPrefixAsync(blobName + Delimiter, cancellationToken);
    }

    private async Task<bool> HasAnyBlobWithPrefixAsync(
        string prefix,
        CancellationToken cancellationToken)
    {
        await foreach (var _ in _containerClient.GetBlobsAsync(
            prefix: prefix,
            cancellationToken: cancellationToken))
        {
            return true;
        }

        return false;
    }

    private static async Task<BlobProperties?> TryGetPropertiesAsync(
        BlobClient blobClient,
        CancellationToken cancellationToken)
    {
        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var response = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        return response.Value;
    }

    private static FileItem MapDirectory(string prefix)
    {
        return new FileItem(
            Name: GetName(prefix),
            Path: ToVirtualPath(prefix),
            Type: FileSystemEntryType.Directory,
            Size: null,
            LastModified: DateTimeOffset.MinValue);
    }

    private static FileItem MapFile(BlobItem blob)
    {
        return new FileItem(
            Name: GetName(blob.Name),
            Path: ToVirtualPath(blob.Name),
            Type: FileSystemEntryType.File,
            Size: blob.Properties.ContentLength,
            LastModified: blob.Properties.LastModified ?? DateTimeOffset.MinValue);
    }

    /// <summary>
    /// Converts a client-facing "/"-rooted path into the blob-name prefix
    /// used to list the entries directly inside it (empty string = container root).
    /// </summary>
    private static string ToBlobPrefix(string? path)
    {
        var blobName = ToBlobName(path);

        return string.IsNullOrEmpty(blobName)
            ? string.Empty
            : blobName.TrimEnd('/') + Delimiter;
    }

    /// <summary>
    /// Converts a client-facing "/"-rooted path into a blob name
    /// (no leading/trailing slash), rejecting attempts to escape via "..".
    /// </summary>
    private static string ToBlobName(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "/")
        {
            return string.Empty;
        }

        var trimmed = path.Trim('/');

        if (trimmed.Split('/').Any(segment => segment is "." or ".."))
        {
            throw new UnauthorizedAccessException(
                "The requested path is not valid.");
        }

        return trimmed;
    }

    private static string ToVirtualPath(string blobNameOrPrefix)
    {
        var trimmed = blobNameOrPrefix.Trim('/');
        return string.IsNullOrEmpty(trimmed) ? "/" : "/" + trimmed;
    }

    private static string GetName(string blobNameOrPrefix)
    {
        var trimmed = blobNameOrPrefix.TrimEnd('/');
        var lastSlash = trimmed.LastIndexOf('/');
        return lastSlash >= 0 ? trimmed[(lastSlash + 1)..] : trimmed;
    }
}
