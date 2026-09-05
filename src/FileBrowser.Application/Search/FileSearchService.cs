using FileBrowser.Application.Abtractstions.Indexing;

using FileBrowser.Application.Abtractstions.AI;
using Microsoft.Extensions.AI;
using FileBrowser.Application.Search.Prompts;
using FileBrowser.Application.Search.Scorer;
using FileBrowser.Application.Search.Semantic;

namespace FileBrowser.Application.Search;

public class FileSearchService : IFileSearchService
{
    private readonly IFileSearchIndex _searchIndex;

    private readonly IFileSearchScorer _searchScorer;
    private readonly ICosineSimilarity _cosineSimilarity;
    private readonly IEmbeddingService _embeddingService;
    private readonly IChatClient _chatClient;
    private readonly IFileSearchPromptBuilder _promptBuilder;

    public FileSearchService(
        IFileSearchIndex searchIndex,
        IFileSearchScorer searchScorer,
        ICosineSimilarity cosineSimilarity,
        IEmbeddingService embeddingService,
        IChatClient chatClient,
        IFileSearchPromptBuilder promptBuilder)
    {
        _searchIndex = searchIndex;
        _searchScorer = searchScorer;
        _cosineSimilarity = cosineSimilarity;
        _embeddingService = embeddingService;
        _chatClient = chatClient;
        _promptBuilder = promptBuilder;
    }

    public async Task<SemanticFileSearchResponseDto> SearchSemanticAsync(
        string query, int limit = 50, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(query))
        {
            return new("Enter a search term to find files and folders.", []);
        }

        var entries = _searchIndex.Snapshot
            .Where(entry => IsUsableEmbedding(entry.Embedding))
            .ToArray();
        
        if (entries.Length == 0)
        {
            return new("No files or folders are available for semantic search yet.", []);
        }

        // Normalize the query using the chat client to improve search results.
        var response = await _chatClient.GetResponseAsync(
            _promptBuilder.BuildQueryNormalizationPrompt(query), 
            cancellationToken: cancellationToken);

        var searchText = string.IsNullOrWhiteSpace(response.Text) ? query.Trim() : response.Text.Trim();
        var queryEmbedding = await _embeddingService.GenerateAsync(searchText, cancellationToken);

        if (!IsUsableEmbedding(queryEmbedding))
        {
            throw new InvalidOperationException("The embedding service returned an invalid query embedding.");
        }

        var results = entries
            .Where(entry => entry.Embedding!.Length == queryEmbedding.Length)
            .Select(entry =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return (
                    Entry: entry, 
                    Score: _cosineSimilarity.Calculate(queryEmbedding, entry.Embedding!));
            })
            .OrderByDescending(result => result.Score)
                .ThenBy(result => result.Entry.RelativePath, StringComparer.Ordinal)
            .Take(limit)
            .Select(result 
                => Map(result.Entry, BuildMatchReason(result.Entry, query.Trim())))
            .ToArray();

        var message = await GenerateSummaryAsync(
            query.Trim(), 
            results, cancellationToken);
        
        return new(message, results);
    }

    private static string BuildMatchReason(FileIndexEntry entry, string query)
    {
        if (entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return $"Matches \"{query}\" in the {(entry.IsDirectory ? "folder" : "file")} name";
        }

        var folder = GetParentPath(entry.RelativePath).Split('/', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(part => part.Contains(query, StringComparison.OrdinalIgnoreCase));
        return folder is not null
            ? $"Located under the \"{folder}\" folder"
            : "Semantic match based on name and folder path";
    }

    private async Task<string> GenerateSummaryAsync(
        string query, IReadOnlyList<FileSearchResultDto> results, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (results.Count == 0)
        {
            return $"No files or folders were found for \"{query}\".";
        }

        var files = results.Count(result => !result.IsDirectory);
        var folders = results.Count - files;
        var items = files == 0 ? CountLabel(folders, "folder")
            : folders == 0 ? CountLabel(files, "file")
            : $"{CountLabel(files, "file")} and {CountLabel(folders, "folder")}";
        var fallback = $"Found {items}.";

        try
        {
            var response = await _chatClient.GetResponseAsync(
                _promptBuilder.BuildResultsSummaryPrompt(query, results), cancellationToken: cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return string.IsNullOrWhiteSpace(response.Text) ? fallback : response.Text.Trim();
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Explanation is optional: preserve retrieved results on provider failures/timeouts.
            return fallback;
        }
    }
    private static string CountLabel(int count, string noun) => $"{count} {noun}{(count == 1 ? "" : "s")}";

    private static string GetParentPath(string path)
    {
        var normalized = path.Replace('\\', '/').Trim('/');
        var separator = normalized.LastIndexOf('/');
        return separator < 0 ? "" : normalized[..separator];
    }

    internal static bool IsUsableEmbedding(float[]? embedding) =>
        embedding is { Length: > 0 } && embedding.All(float.IsFinite) && embedding.Any(value => value != 0);

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
        FileIndexEntry entry, string? matchReason = null)
    {
        return new FileSearchResultDto
        {
            Name = entry.Name,
            Path = entry.RelativePath,
            IsDirectory = entry.IsDirectory,
            Extension = entry.Extension,
            MatchReason = matchReason
        };
    }
}
