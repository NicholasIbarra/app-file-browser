using FileBrowser.Application.Abtractstions.Indexing;
using FileBrowser.Application.Search;

namespace FileBrowser.Application.Tests.Search;

public sealed class FileSearchScorerTests
{
    private readonly FileSearchScorer _sut = new();

    [Fact]
    public void Score_NullEntry_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _sut.Score(null!, "report"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Score_NullOrWhitespaceQuery_ReturnsNull(string? query)
    {
        var result = _sut.Score(CreateEntry("report.txt"), query!);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("REPORT-BUDGET.TXT", 1000, FileSearchMatchType.ExactName)]
    [InlineData("rep", 800, FileSearchMatchType.NamePrefix)]
    [InlineData("bud", 600, FileSearchMatchType.WordPrefix)]
    [InlineData("port", 400, FileSearchMatchType.NameContains)]
    public void Score_NameMatch_ReturnsExpectedMatch(
        string query,
        int expectedScore,
        FileSearchMatchType expectedMatchType)
    {
        var entry = CreateEntry("Report-Budget.txt");

        var result = _sut.Score(entry, query);

        Assert.NotNull(result);
        Assert.Equal(expectedScore, result.Score);
        Assert.Equal(expectedMatchType, result.MatchType);
    }

    [Fact]
    public void Score_QueryOnlyInPath_ReturnsPathContainsMatch()
    {
        var entry = CreateEntry("report.txt", "/Finance/Quarterly/report.txt");

        var result = _sut.Score(entry, "quarterly");

        Assert.Equal(new FileSearchMatch(200, FileSearchMatchType.PathContains), result);
    }

    [Fact]
    public void Score_QueryAfterLetterOrDigit_IsNotTreatedAsWordPrefix()
    {
        var entry = CreateEntry("annualreport.txt");

        var result = _sut.Score(entry, "report");

        Assert.Equal(new FileSearchMatch(400, FileSearchMatchType.NameContains), result);
    }

    [Fact]
    public void Score_NoMatch_ReturnsNull()
    {
        var result = _sut.Score(CreateEntry("report.txt", "/Finance/report.txt"), "photo");

        Assert.Null(result);
    }

    private static FileIndexEntry CreateEntry(
        string name,
        string? relativePath = null) =>
        new(name, relativePath ?? $"/{name}", $"C:\\Files\\{name}", false, Path.GetExtension(name));
}
