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

    public async Task AddOrUpdateAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await _rebuildLock.WaitAsync(cancellationToken);

        try
        {
            var relativePath = NormalizeRelativePath(path);
            var fullPath = GetFullPath(relativePath);
            var entry = GetExistingEntry(fullPath, path);
            var indexedEntries = BuildEntries(entry, cancellationToken);

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
            var destinationEntry = GetExistingEntry(
                GetFullPath(destination),
                destinationPath);
            var movedEntries = BuildEntries(destinationEntry, cancellationToken);

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

            results.Add(Map(entry));
        }

        return results;
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

    internal string GetFullPath(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(
            _rootPath,
            relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var pathFromRoot = Path.GetRelativePath(_rootPath, fullPath);

        if (Path.IsPathRooted(pathFromRoot)
            || pathFromRoot.Equals("..", StringComparison.Ordinal)
            || pathFromRoot.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException(
                $"Path '{relativePath}' resolves outside the indexed root.");
        }

        return fullPath;
    }

    private FileIndexEntry Map(FileSystemInfo entry)
    {
        var isDirectory = (entry.Attributes & FileAttributes.Directory) != 0;

        return new FileIndexEntry(
            Name: entry.Name,
            RelativePath: Path.GetRelativePath(_rootPath, entry.FullName),
            FullPath: entry.FullName,
            IsDirectory: isDirectory,
            Extension: isDirectory ? null : Path.GetExtension(entry.Name));
    }

    internal static FileSystemInfo GetExistingEntry(string fullPath, string originalPath)
    {
        if (File.Exists(fullPath))
        {
            return new FileInfo(fullPath);
        }

        if (Directory.Exists(fullPath))
        {
            return new DirectoryInfo(fullPath);
        }

        throw new FileNotFoundException(
            $"Cannot add '{originalPath}' to the index because it does not exist.",
            originalPath);
    }

    private IReadOnlyList<FileIndexEntry> BuildEntries(
        FileSystemInfo rootEntry,
        CancellationToken cancellationToken)
    {
        var entries = new List<FileIndexEntry> { Map(rootEntry) };

        if (rootEntry is not DirectoryInfo directory)
        {
            return entries;
        }

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        foreach (var entry in directory.EnumerateFileSystemInfos("*", options))
        {
            cancellationToken.ThrowIfCancellationRequested();
            entries.Add(Map(entry));
        }

        return entries;
    }

    public void Dispose()
    {
        _rebuildLock.Dispose();
    }

}
