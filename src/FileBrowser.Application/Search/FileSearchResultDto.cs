using FileBrowser.Application.Abtractstions.FileSystem;

namespace FileBrowser.Application.Search;

public sealed class FileSearchResultDto
{
    public required string Name { get; init; }

    public required string Path { get; init; }

    public bool IsDirectory { get; init; }

    public string? Extension { get; init; }

    public long? Size { get; init; }

    public DateTimeOffset LastModified { get; init; }

    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? MatchReason { get; init; }
}
