using FileBrowser.Application.Abtractstions.Indexing;
using FileBrowser.Infrastructure.FileSystem;
using Microsoft.Extensions.Options;

namespace FileBrowser.Infrastructure.Indexing;

public class FileSearchIndex : IFileSearchIndex, IDisposable
{
    private readonly string _rootPath;
    private readonly SemaphoreSlim _rebuildLock = new(1, 1);

    private IReadOnlyList<FileIndexEntry> _snapshot =
        Array.Empty<FileIndexEntry>();


    public FileSearchIndex(IOptions<FileBrowserOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _rootPath = Path.GetFullPath(options.Value.HomeDirectory);
    }

    /// <summary>
    /// Volatile.Write means readers see either the entire old index or entire new index. 
    /// They never observe an index halfway through being rebuilt.
    /// @nib: review
    /// </summary>
    public IReadOnlyList<FileIndexEntry> Snapshot => Volatile.Read(ref _snapshot);

    public async Task RebuildAsync(CancellationToken cancellationToken)
    {
        await _rebuildLock.WaitAsync(cancellationToken);

        try
        {
            var entries = await Task.Run(
                () => BuildIndex(cancellationToken),
                cancellationToken);

            // Searches continue using the old snapshot until
            // the complete new index is ready.
            Volatile.Write(ref _snapshot, entries);
        }
        finally
        {
            _rebuildLock.Release();
        }
    }

    public async Task RemoveAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await _rebuildLock.WaitAsync(cancellationToken);

        try
        {
            var deletedPath = NormalizeRelativePath(path);
            var descendantPrefix = deletedPath + "/";

            var entries = Snapshot
                .Where(entry =>
                {
                    var entryPath = NormalizeRelativePath(entry.RelativePath);

                    return !entryPath.Equals(deletedPath, StringComparison.OrdinalIgnoreCase)
                        && !entryPath.StartsWith(descendantPrefix, StringComparison.OrdinalIgnoreCase);
                })
                .ToArray();

            Volatile.Write(ref _snapshot, entries);
        }
        finally
        {
            _rebuildLock.Release();
        }
    }

    public IReadOnlyList<FileIndexEntry> BuildIndex(CancellationToken cancellationToken)
    {
        var results = new List<FileIndexEntry>();

        var root = new DirectoryInfo(_rootPath);

        if (!root.Exists)
        {
            return Array.Empty<FileIndexEntry>();
        }

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.ReparsePoint // Avoid following directory junctions / symlinks
        };

        foreach(var entry in root.EnumerateFileSystemInfos("*", options))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var isDirectory = (entry.Attributes & FileAttributes.Directory) != 0;

            results.Add(new FileIndexEntry(
                Name: entry.Name,
                RelativePath: Path.GetRelativePath(_rootPath, entry.FullName),
                FullPath: entry.FullName,
                IsDirectory: isDirectory,
                Extension: isDirectory ? null : Path.GetExtension(entry.Name)));
        }

        return results;
    }

    private static string NormalizeRelativePath(string path)
    {
        return path
            .Replace('\\', '/')
            .Trim('/');
    }

    public void Dispose()
    {
        _rebuildLock.Dispose();
    }

}
