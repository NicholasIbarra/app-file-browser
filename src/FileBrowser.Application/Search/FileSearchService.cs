using FileBrowser.Application.Abtractstions.Indexing;

namespace FileBrowser.Application.Search;

public class FileSearchService : IFileSearchService
{
    private readonly IFileSearchIndex _searchIndex;

    private readonly IFileSearchScorer _searchScorer;

    public FileSearchService(IFileSearchIndex searchIndex, IFileSearchScorer searchScorer)
    {
        _searchIndex = searchIndex;
        _searchScorer = searchScorer;
    }

    public IReadOnlyList<FileSearchResultDto> SearchAsync(string query, int limit = 50, CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                "Limit must be greater than zero.");
        }

        if (query is null || string.IsNullOrEmpty(query.Trim()))
        {
            return [];
        }

        var normalizedQuery = query.Trim();

        var results = _searchIndex
            .Snapshot
            .Select(entry =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var score = _searchScorer.Score(entry, normalizedQuery);

                return new
                {
                    Entry = entry,
                    Match = score
                };
            })
            .Where(x => x.Match is not null)
            .OrderByDescending(x => x.Match!.Score)
                 .ThenBy(x => x.Entry.Name.Length)
            .Take(limit)
            .Select(x => Map(x.Entry))
            .ToArray();

        return results;
    }

    private static FileSearchResultDto Map(
        FileIndexEntry entry)
    {
        return new FileSearchResultDto
        {
            Name = entry.Name,
            Path = entry.RelativePath,
            IsDirectory = entry.IsDirectory,
            Extension = entry.Extension
        };
    }
}