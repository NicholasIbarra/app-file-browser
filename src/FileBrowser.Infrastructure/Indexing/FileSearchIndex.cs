using FileBrowser.Application.Abtractstions.FileSystem;
using FileBrowser.Application.Abtractstions.Indexing;

namespace FileBrowser.Infrastructure.Indexing;

public class FileSearchIndex : IFileSearchIndex, IDisposable
{
    private readonly SemaphoreSlim _rebuildLock = new(1, 1);

    private readonly IFileSystem _fileSystem;

    private IReadOnlyList<FileIndexEntry> _snapshot =
        Array.Empty<FileIndexEntry>();


    public FileSearchIndex(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
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
            var entries = await BuildIndexAsync(cancellationToken);

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

    public async Task AddOrUpdateAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await _rebuildLock.WaitAsync(cancellationToken);

        try
        {
            var relativePath = NormalizeRelativePath(path);
            var indexedEntries = await BuildEntriesAsync(path, cancellationToken);

            var entries = Snapshot
                .Where(existing => !IsPathOrDescendant(existing.RelativePath, relativePath))
                .Concat(indexedEntries)
                .ToArray();

            Volatile.Write(ref _snapshot, entries);
        }
        finally
        {
            _rebuildLock.Release();
        }
    }

    public async Task MoveAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        await _rebuildLock.WaitAsync(cancellationToken);

        try
        {
            var source = NormalizeRelativePath(sourcePath);
            var destination = NormalizeRelativePath(destinationPath);
            var movedEntries = await BuildEntriesAsync(destinationPath, cancellationToken);

            var entries = Snapshot
                .Where(existing =>
                    !IsPathOrDescendant(existing.RelativePath, source)
                    && !IsPathOrDescendant(existing.RelativePath, destination))
                .Concat(movedEntries)
                .ToArray();

            Volatile.Write(ref _snapshot, entries);
        }
        finally
        {
            _rebuildLock.Release();
        }
    }

    internal async Task<IReadOnlyList<FileIndexEntry>> BuildIndexAsync(CancellationToken cancellationToken)
    {
        var contents = await _fileSystem.GetAllDirectoryContentsAsync("/", cancellationToken);
        return contents.Select(Map).ToArray();
    }

    internal static string NormalizeRelativePath(string path)
    {
        return path
            .Replace('\\', '/')
            .Trim('/');
    }

    internal static bool IsPathOrDescendant(string candidatePath, string parentPath)
    {
        var candidate = NormalizeRelativePath(candidatePath);

        return candidate.Equals(parentPath, StringComparison.OrdinalIgnoreCase)
            || candidate.StartsWith(parentPath + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static FileIndexEntry Map(FileItem entry)
    {
        var isDirectory = entry.Type == FileSystemEntryType.Directory;

        return new FileIndexEntry(
            Name: entry.Name,
            RelativePath: NormalizeRelativePath(entry.Path),
            FullPath: entry.Path,
            IsDirectory: isDirectory,
            Extension: isDirectory ? null : Path.GetExtension(entry.Name));
    }

    private async Task<IReadOnlyList<FileIndexEntry>> BuildEntriesAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var rootEntry = await _fileSystem.GetFileAsync(path, cancellationToken);

        if (rootEntry is null)
        {
            throw new FileNotFoundException(
                $"Cannot add '{path}' to the index because it does not exist.",
                path);
        }

        if (rootEntry.Type != FileSystemEntryType.Directory)
        {
            return [Map(rootEntry)];
        }

        var descendants = await _fileSystem.GetAllDirectoryContentsAsync(path, cancellationToken);
        return descendants.Prepend(rootEntry).Select(Map).ToArray();
    }

    public void Dispose()
    {
        _rebuildLock.Dispose();
    }

}
