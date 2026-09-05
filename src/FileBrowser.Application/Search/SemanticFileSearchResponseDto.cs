namespace FileBrowser.Application.Search;

public sealed record SemanticFileSearchResponseDto(
    string Message,
    IReadOnlyList<FileSearchResultDto> Results);
