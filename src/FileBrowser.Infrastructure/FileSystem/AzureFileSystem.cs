using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FileBrowser.Application.Abtractstions.FileSystem;
using Microsoft.Extensions.Options;

namespace FileBrowser.Infrastructure.FileSystem;

public sealed class AzureFileSystem : IFileSystem
{
    private readonly BlobContainerClient _container;
    private readonly string _rootPrefix;

    public AzureFileSystem(BlobContainerClient container, IOptions<FileBrowserOptions> options)
    {
        _container = container;
        _rootPrefix = NormalizePrefix(options.Value.Azure.Prefix);
    }

    public async Task<DirectoryContents> GetDirectoryContentsAsync(string path, CancellationToken cancellationToken = default)
    {
        var relativePath = NormalizePath(path);
        var prefix = ToDirectoryPrefix(relativePath);
        var entries = new List<FileItem>();

        await foreach (var item in _container.GetBlobsByHierarchyAsync(BlobTraits.None, BlobStates.None, "/", prefix, cancellationToken))
        {
            if (item.IsPrefix)
            {
                var childCount = await GetChildCountAsync(item.Prefix, cancellationToken);
                entries.Add(MapDirectory(item.Prefix, childCount));
            }
            else if (item.IsBlob && !IsDirectoryMarker(item.Blob.Name))
            {
                entries.Add(MapBlob(item.Blob));
            }
        }

        if (relativePath.Length > 0 && entries.Count == 0 && !await DirectoryExistsAsync(relativePath, cancellationToken))
        {
            throw new DirectoryNotFoundException($"Directory '{path}' was not found.");
        }

        return new DirectoryContents(ToPublicPath(relativePath), entries);
    }

    public async Task<IReadOnlyList<FileItem>> GetAllDirectoryContentsAsync(string? path, CancellationToken cancellationToken = default)
    {
        var relativePath = NormalizePath(path);
        var prefix = ToDirectoryPrefix(relativePath);
        var entries = new List<FileItem>();
        var directories = new HashSet<string>(StringComparer.Ordinal);

        await foreach (var blob in _container.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, cancellationToken))
        {
            if (!IsDirectoryMarker(blob.Name))
            {
                entries.Add(MapBlob(blob));
            }

            AddParentDirectories(blob.Name, prefix, directories, entries);
        }

        return entries;
    }

    public async Task<FileItem?> GetFileAsync(string path, CancellationToken cancellationToken = default)
    {
        var relativePath = NormalizePath(path);
        if (relativePath.Length == 0)
        {
            return new FileItem("/", "/", FileSystemEntryType.Directory, null, null, DateTimeOffset.MinValue);
        }

        var client = _container.GetBlobClient(ToBlobName(relativePath));
        try
        {
            var properties = await client.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return new FileItem(GetName(relativePath), ToPublicPath(relativePath), FileSystemEntryType.File,
                properties.Value.ContentLength, null, properties.Value.LastModified);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Blob names are exact; if there is no blob, the path may still be a virtual directory.
        }

        return await DirectoryExistsAsync(relativePath, cancellationToken)
            ? new FileItem(GetName(relativePath), ToPublicPath(relativePath), FileSystemEntryType.Directory, null, null, DateTimeOffset.MinValue)
            : null;
    }

    public async Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _container.GetBlobClient(ToBlobName(NormalizeRequiredPath(path)))
                .OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            throw new FileNotFoundException($"File '{path}' was not found.", path, ex);
        }
    }

    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        var relativePath = NormalizePath(path);
        if (relativePath.Length == 0) return true;
        if (await _container.GetBlobClient(ToBlobName(relativePath)).ExistsAsync(cancellationToken).ConfigureAwait(false)) return true;
        return await DirectoryExistsAsync(relativePath, cancellationToken);
    }

    public async Task UploadAsync(string path, Stream content, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var blob = _container.GetBlobClient(ToBlobName(NormalizeRequiredPath(path)));
        try
        {
            await blob.UploadAsync(content, overwrite, cancellationToken).ConfigureAwait(false);
        }
        catch (RequestFailedException ex) when (ex.Status == 409 || ex.Status == 412)
        {
            throw new IOException($"An entry already exists at '{path}'.", ex);
        }
    }

    public async Task DeleteAsync(string path, bool recursive = false, CancellationToken cancellationToken = default)
    {
        var relativePath = NormalizeRequiredPath(path);
        var blob = _container.GetBlobClient(ToBlobName(relativePath));
        if (await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken).ConfigureAwait(false)) return;

        var children = await GetBlobNamesAsync(ToDirectoryPrefix(relativePath), cancellationToken);
        if (children.Count == 0) throw new FileNotFoundException($"File or directory '{path}' was not found.", path);
        if (!recursive) throw new IOException($"Directory '{path}' is not empty.");
        foreach (var name in children)
        {
            await _container.DeleteBlobIfExistsAsync(name, DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task MoveAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        await CopyAsync(sourcePath, destinationPath, overwrite, cancellationToken);
        await DeleteAsync(sourcePath, recursive: true, cancellationToken);
    }

    public async Task CopyAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var source = NormalizeRequiredPath(sourcePath);
        var destination = NormalizeRequiredPath(destinationPath);
        if (destination.StartsWith(source + "/", StringComparison.Ordinal))
            throw new IOException("A directory cannot be copied into itself.");

        var sourceBlob = _container.GetBlobClient(ToBlobName(source));
        if (await sourceBlob.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            await CopyBlobAsync(sourceBlob, _container.GetBlobClient(ToBlobName(destination)), overwrite, cancellationToken);
            return;
        }

        var sourcePrefix = ToDirectoryPrefix(source);
        var names = await GetBlobNamesAsync(sourcePrefix, cancellationToken);
        if (names.Count == 0) throw new FileNotFoundException($"File or directory '{sourcePath}' was not found.", sourcePath);
        foreach (var name in names)
        {
            var suffix = name[sourcePrefix.Length..];
            await CopyBlobAsync(_container.GetBlobClient(name), _container.GetBlobClient(ToDirectoryPrefix(destination) + suffix), overwrite, cancellationToken);
        }
    }

    private static async Task CopyBlobAsync(BlobClient source, BlobClient destination, bool overwrite, CancellationToken cancellationToken)
    {
        if (!overwrite && await destination.ExistsAsync(cancellationToken).ConfigureAwait(false))
            throw new IOException($"An entry already exists at '{destination.Name}'.");
        await using var stream = await source.OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        await destination.UploadAsync(stream, overwrite, cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<string>> GetBlobNamesAsync(string prefix, CancellationToken cancellationToken)
    {
        var names = new List<string>();
        await foreach (var blob in _container.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, cancellationToken)) names.Add(blob.Name);
        return names;
    }

    private async Task<bool> DirectoryExistsAsync(string path, CancellationToken cancellationToken)
    {
        await foreach (var page in _container.GetBlobsAsync(BlobTraits.None, BlobStates.None, ToDirectoryPrefix(path), cancellationToken).AsPages(pageSizeHint: 1))
        {
            if (page.Values.Count > 0) return true;
        }
        return false;
    }

    private async Task<int> GetChildCountAsync(string prefix, CancellationToken cancellationToken)
    {
        var count = 0;

        await foreach (var item in _container.GetBlobsByHierarchyAsync(
                           BlobTraits.None,
                           BlobStates.None,
                           "/",
                           prefix,
                           cancellationToken))
        {
            if (item.IsPrefix || item.IsBlob && !IsDirectoryMarker(item.Blob.Name))
            {
                count++;
            }
        }

        return count;
    }

    private FileItem MapBlob(BlobItem blob)
    {
        var path = FromBlobName(blob.Name);
        return new FileItem(
            GetName(path),
            ToPublicPath(path),
            FileSystemEntryType.File,
            blob.Properties.ContentLength,
            null,
            blob.Properties.LastModified ?? DateTimeOffset.MinValue);
    }

    private FileItem MapDirectory(string prefix, int? childCount = null)
    {
        var path = FromBlobName(prefix.TrimEnd('/'));
        return new FileItem(
            GetName(path),
            ToPublicPath(path),
            FileSystemEntryType.Directory,
            null,
            childCount,
            DateTimeOffset.MinValue);
    }

    private void AddParentDirectories(string blobName, string listingPrefix, HashSet<string> seen, List<FileItem> entries)
    {
        var remainder = blobName[listingPrefix.Length..].TrimEnd('/');
        var segments = remainder.Split('/');
        for (var i = 1; i < segments.Length; i++)
        {
            var name = listingPrefix + string.Join('/', segments.Take(i));
            if (seen.Add(name)) entries.Add(MapDirectory(name));
        }
    }
    public Task RenameAsync(string sourcePath, string newName, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        // Rename is implemented as a move operation to the same directory with a new name.
        return MoveAsync(sourcePath, Path.Combine(Path.GetDirectoryName(sourcePath) ?? string.Empty, newName), overwrite, cancellationToken);
    }

    private string ToBlobName(string path) => _rootPrefix + path;
    private string ToDirectoryPrefix(string path) => path.Length == 0 ? _rootPrefix : ToBlobName(path) + "/";
    private string FromBlobName(string name) => name[_rootPrefix.Length..];
    private bool IsDirectoryMarker(string name) => name.EndsWith('/');
    private static string NormalizePrefix(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : NormalizePath(value) + "/";
    private static string NormalizeRequiredPath(string? path) => NormalizePath(path) is { Length: > 0 } value ? value : throw new InvalidOperationException("The file system root cannot be modified.");

    private static string NormalizePath(string? path)
    {
        var value = (path ?? string.Empty).Replace('\\', '/').Trim('/');
        if (value.Split('/', StringSplitOptions.RemoveEmptyEntries).Any(segment => segment is "." or ".."))
            throw new UnauthorizedAccessException("The path must remain within the configured container prefix.");
        return value;
    }

    private static string ToPublicPath(string path) => path.Length == 0 ? "/" : "/" + path;
    private static string GetName(string path) => path[(path.LastIndexOf('/') + 1)..];
}
