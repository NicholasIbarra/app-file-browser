using FileBrowser.Application.Abtractstions.FileSystem;
using FileBrowser.Application.Abtractstions.Indexing;

namespace FileBrowser.Infrastructure.Indexing;

public class FileSearchIndex : IFileSearchIndex, IDisposable
{
    private readonly IFileSystem _fileSystem;
    private readonly SemaphoreSlim _rebuildLock = new(1, 1);

    private IReadOnlyList<FileIndexEntry> _snapshot =
        Array.Empty<FileIndexEntry>();


    public FileSearchIndex(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);

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

    public async Task<IReadOnlyList<FileIndexEntry>> BuildIndexAsync(CancellationToken cancellationToken)
    {
        var results = new List<FileIndexEntry>();

        await WalkAsync("/", results, cancellationToken);

        return results;
    }

    private async Task WalkAsync(
        string path,
        List<FileIndexEntry> results,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var contents = await _fileSystem.GetDirectoryContentsAsync(path, cancellationToken);

        foreach (var entry in contents.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var isDirectory = entry.Type == FileSystemEntryType.Directory;

            results.Add(new FileIndexEntry(
                Name: entry.Name,
                RelativePath: entry.Path,
                FullPath: entry.Path,
                IsDirectory: isDirectory,
                Extension: isDirectory ? null : Path.GetExtension(entry.Name)));

            if (isDirectory)
            {
                await WalkAsync(entry.Path, results, cancellationToken);
            }
        }
    }

    public void Dispose()
    {
        _rebuildLock.Dispose();
    }

}
