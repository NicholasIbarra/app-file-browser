namespace FileBrowser.Application.Abtractstions.Indexing;

public interface IFileSearchIndex
{
    IReadOnlyList<FileIndexEntry> Snapshot { get; }

    Task RebuildAsync(CancellationToken cancellationToken);
}
