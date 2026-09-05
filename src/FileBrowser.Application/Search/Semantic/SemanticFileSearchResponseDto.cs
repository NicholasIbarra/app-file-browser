namespace FileBrowser.Application.Search.Semantic;

public sealed record SemanticFileSearchResponseDto(
    string Message,
    IReadOnlyList<FileSearchResultDto> Results);
