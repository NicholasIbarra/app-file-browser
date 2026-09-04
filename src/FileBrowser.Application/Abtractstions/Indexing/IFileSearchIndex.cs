namespace FileBrowser.Application.Abtractstions.Indexing;

public interface IFileSearchIndex
{
    IReadOnlyList<FileIndexEntry> Snapshot { get; }

    Task RebuildAsync(CancellationToken cancellationToken);

    Task RemoveAsync(string path, CancellationToken cancellationToken);
}
