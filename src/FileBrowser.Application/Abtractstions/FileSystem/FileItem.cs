namespace FileBrowser.Application.Abtractstions.FileSystem;

public sealed record FileItem(
    string Name,
    string Path,
    FileSystemEntryType Type,
    long? Size,
    DateTimeOffset LastModified);
