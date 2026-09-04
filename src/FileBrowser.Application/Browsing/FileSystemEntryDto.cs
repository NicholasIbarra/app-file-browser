using FileBrowser.Application.Abtractstions.FileSystem;

namespace FileBrowser.Application.Browsing;

public sealed record FileSystemEntryDto(
    string Name,
    string Path,
    FileSystemEntryType Type,
    long? Size,
    int? ChildCount,
    DateTimeOffset LastModified);
