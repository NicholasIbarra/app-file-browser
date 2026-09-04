namespace FileBrowser.Application.Abtractstions.FileSystem;

public sealed record DirectoryContents(
    string Path,
    IReadOnlyList<FileItem> Entries);
