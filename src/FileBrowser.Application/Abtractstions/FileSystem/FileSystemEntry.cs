namespace FileBrowser.Application.Abtractstions.FileSystem;

public sealed record FileSystemEntry(
    string Name,
    string Path,
    FileSystemEntryType Type,
    long? Size,
    DateTimeOffset LastModified);
