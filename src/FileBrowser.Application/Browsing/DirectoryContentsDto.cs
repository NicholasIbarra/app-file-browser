namespace FileBrowser.Application.Browsing;

public sealed record DirectoryContentsDto(
    string Path,
    IReadOnlyList<FileSystemEntryDto> Entries);
