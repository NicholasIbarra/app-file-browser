namespace FileBrowser.Application.Abtractstions.FileSystem;

public sealed record FileItem(
    string Name,
    string Path,
    FileSystemEntryType Type,
    long? Size,
    int? ChildCount,
    DateTimeOffset LastModified);
