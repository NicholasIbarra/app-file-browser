namespace FileBrowser.Application.Search.Scorer;

public sealed record FileSearchMatch(
    int Score,
    FileSearchMatchType MatchType);
