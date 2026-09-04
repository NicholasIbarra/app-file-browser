namespace FileBrowser.Application.Search;

public sealed record FileSearchMatch(
    int Score,
    FileSearchMatchType MatchType);
