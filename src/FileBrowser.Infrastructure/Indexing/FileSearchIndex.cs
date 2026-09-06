using FileBrowser.Application.Abtractstions.AI;
using FileBrowser.Application.Abtractstions.FileSystem;
using FileBrowser.Application.Abtractstions.Indexing;
using FileBrowser.Infrastructure.AI;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace FileBrowser.Infrastructure.Indexing;

public class FileSearchIndex : IFileSearchIndex, IDisposable
{
    private readonly SemaphoreSlim _rebuildLock = new(1, 1);

    private IReadOnlyList<FileIndexEntry> _snapshot =
        Array.Empty<FileIndexEntry>();

    private const int MAX_EMBEDDING_BATCH_SIZE = 500;

    private readonly IFileSystem _fileSystem;

    private readonly IEmbeddingService _embeddingService;

    private readonly bool _embeddingEnabled;
    private readonly ILogger<FileSearchIndex> _logger;

    public FileSearchIndex(IFileSystem fileSystem, IEmbeddingService embeddingService, IOptions<AzureOpenAiOptions> options, ILogger<FileSearchIndex> logger)
    {
        _fileSystem = fileSystem;
        _embeddingService = embeddingService;
        _embeddingEnabled = options.Value.Enabled;
        _logger = logger;
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
            var stopwatch = Stopwatch.StartNew();
            var entries = await BuildIndexAsync(cancellationToken);

            // Searches continue using the old snapshot until
            // the complete new index is ready.
            Volatile.Write(ref _snapshot, entries);
            stopwatch.Stop();

            _logger.LogInformation(
                "File search index rebuilt: {TotalRecordsIndexed} records indexed in {ElapsedMilliseconds} ms.",
                entries.Count,
                stopwatch.Elapsed.TotalMilliseconds);
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

        var entries = contents.Select(Map).ToArray();

        return _embeddingEnabled
            ? await GenerateEmbeddings(entries, cancellationToken)
            : entries;
    }

    private async Task<IReadOnlyList<FileIndexEntry>> GenerateEmbeddings(
        IReadOnlyList<FileIndexEntry> entries,
        CancellationToken cancellationToken)
    {
        if (!_embeddingEnabled)
        {
            return entries;
        }

        var results = new FileIndexEntry[entries.Count];

        var batches = entries
            .Select((entry, index) => (entry, index))
            .Chunk(MAX_EMBEDDING_BATCH_SIZE);

        await Parallel.ForEachAsync(
            batches,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = 4,
                CancellationToken = cancellationToken
            },
            async (batch, ct) =>
            {
                var inputs = batch
                    .Select(x => BuildEmbeddingText(x.entry))
                    .ToArray();

                var embeddings = await _embeddingService.GenerateAsync(
                    inputs,
                    ct);

                foreach (var (entry, index) in batch)
                {
                    var input = BuildEmbeddingText(entry);

                    results[index] = entry with
                    {
                        Embedding = embeddings[input]
                    };
                }
            });

        return results;
    }

    private static string BuildEmbeddingText(FileIndexEntry entry)
    {
        return $"""
        Name: {entry.Name}
        Path: {entry.RelativePath}
        Type: {(entry.IsDirectory ? "Directory" : "File")}
        Extension: {entry.Extension}
        Size: {(entry.Size is { } size ? FormattableString.Invariant($"{size} bytes ({size / 1024d:0.##} KB, {size / 1048576d:0.##} MB, {size / 1073741824d:0.##} GB)") : "Not applicable")}
        Last modified: {entry.LastModified:O}
        """;
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
            Extension: isDirectory ? null : Path.GetExtension(entry.Name),
            Embedding: null,
            Size: entry.Size,
            LastModified: entry.LastModified);
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
