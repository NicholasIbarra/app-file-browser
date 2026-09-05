using FileBrowser.Application.Abtractstions.Indexing;
using FileBrowser.Application.Search;
using FileBrowser.Application.Search.Prompts;
using NSubstitute;
using FileBrowser.Application.Abtractstions.AI;
using Microsoft.Extensions.AI;
using FileBrowser.Application.Search.Scorer;
using FileBrowser.Application.Search.Semantic;

namespace FileBrowser.Application.Tests.Search;

public sealed class FileSearchServiceTests
{
    private readonly IFileSearchIndex _searchIndex;
    private readonly IFileSearchScorer _searchScorer;
    private readonly FileSearchService _sut;
    private readonly IEmbeddingService _embeddings = Substitute.For<IEmbeddingService>();
    private readonly IChatClient _chat = Substitute.For<IChatClient>();
    private readonly ICosineSimilarity _cosineSimilarity = Substitute.For<ICosineSimilarity>();

    public FileSearchServiceTests()
    {
        _searchIndex = Substitute.For<IFileSearchIndex>();
        _searchScorer = Substitute.For<IFileSearchScorer>();
        _searchIndex.Snapshot.Returns([]);
        _sut = new FileSearchService(
            _searchIndex, _searchScorer, _cosineSimilarity, _embeddings, _chat,
            new FileSearchPromptBuilder());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SearchAsync_NonPositiveLimit_ThrowsArgumentOutOfRangeException(int limit)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _sut.SearchAsync("report", limit));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SearchAsync_NullOrWhitespaceQuery_ReturnsEmptyWithoutScoring(string? query)
    {
        _searchIndex.Snapshot.Returns([CreateEntry("report.txt")]);

        var result = _sut.SearchAsync(query!);

        Assert.Empty(result);
        _searchScorer.DidNotReceiveWithAnyArgs().Score(default!, default!);
    }

    [Fact]
    public void SearchAsync_TrimsQueryBeforeScoring()
    {
        var entry = CreateEntry("report.txt");
        _searchIndex.Snapshot.Returns([entry]);

        _sut.SearchAsync("  report  ");

        _searchScorer.Received(1).Score(entry, "report");
    }

    [Fact]
    public void SearchAsync_FiltersNonMatchesAndOrdersByScoreThenNameLength()
    {
        var noMatch = CreateEntry("unrelated.txt");
        var lowerScore = CreateEntry("report-summary.txt");
        var longerTie = CreateEntry("report-annual.txt");
        var shorterTie = CreateEntry("report.txt");
        _searchIndex.Snapshot.Returns([noMatch, lowerScore, longerTie, shorterTie]);
        _searchScorer.Score(noMatch, "report").Returns((FileSearchMatch?)null);
        _searchScorer.Score(lowerScore, "report").Returns(new FileSearchMatch(400, FileSearchMatchType.NameContains));
        _searchScorer.Score(longerTie, "report").Returns(new FileSearchMatch(800, FileSearchMatchType.NamePrefix));
        _searchScorer.Score(shorterTie, "report").Returns(new FileSearchMatch(800, FileSearchMatchType.NamePrefix));

        var result = _sut.SearchAsync("report");

        Assert.Equal(
            ["report.txt", "report-annual.txt", "report-summary.txt"],
            result.Select(item => item.Name));
    }

    [Fact]
    public void SearchAsync_AppliesLimitAfterOrdering()
    {
        var lowerScore = CreateEntry("lower.txt");
        var higherScore = CreateEntry("higher.txt");
        _searchIndex.Snapshot.Returns([lowerScore, higherScore]);
        _searchScorer.Score(lowerScore, Arg.Any<string>()).Returns(new FileSearchMatch(200, FileSearchMatchType.PathContains));
        _searchScorer.Score(higherScore, Arg.Any<string>()).Returns(new FileSearchMatch(1000, FileSearchMatchType.ExactName));

        var result = _sut.SearchAsync("query", limit: 1);

        Assert.Equal("higher.txt", Assert.Single(result).Name);
    }

    [Fact]
    public void SearchAsync_MapsAllResultFields()
    {
        var entry = new FileIndexEntry("Reports", "/Documents/Reports", "C:\\Files\\Reports", true, null, null);
        _searchIndex.Snapshot.Returns([entry]);
        _searchScorer.Score(entry, Arg.Any<string>()).Returns(new FileSearchMatch(1000, FileSearchMatchType.ExactName));

        var result = Assert.Single(_sut.SearchAsync("Reports"));

        Assert.Equal(entry.Name, result.Name);
        Assert.Equal(entry.RelativePath, result.Path);
        Assert.Equal(entry.IsDirectory, result.IsDirectory);
        Assert.Equal(entry.Extension, result.Extension);
    }

    [Fact]
    public void SearchAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        _searchIndex.Snapshot.Returns([CreateEntry("report.txt")]);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            _sut.SearchAsync("report", cancellationToken: cancellationTokenSource.Token));
    }

    [Fact]
    public async Task SearchSemanticAsync_OrdersResultsUsingInjectedSimilarity()
    {
        float[] queryEmbedding = [1, 0];
        var first = CreateEntry("first.txt") with { Embedding = [1, 0] };
        var second = CreateEntry("second.txt") with { Embedding = [0, 1] };
        _searchIndex.Snapshot.Returns([first, second]);
        _chat.GetResponseAsync(
                Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "query")));
        _embeddings.GenerateAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, float[]> { ["query"] = queryEmbedding });
        _cosineSimilarity.Calculate(queryEmbedding, first.Embedding!).Returns(0.1);
        _cosineSimilarity.Calculate(queryEmbedding, second.Embedding!).Returns(0.9);

        var result = await _sut.SearchSemanticAsync("query");

        Assert.Equal(["second.txt", "first.txt"], result.Results.Select(item => item.Name));
        _cosineSimilarity.Received(1).Calculate(queryEmbedding, first.Embedding!);
        _cosineSimilarity.Received(1).Calculate(queryEmbedding, second.Embedding!);
    }

    private static FileIndexEntry CreateEntry(string name) =>
        new(name, $"/{name}", $"C:\\Files\\{name}", false, Path.GetExtension(name), null);
}
