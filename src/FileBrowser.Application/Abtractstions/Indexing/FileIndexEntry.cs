namespace FileBrowser.Application.Abtractstions.Indexing;

public sealed record FileIndexEntry(
    string Name,
    string RelativePath,
    string FullPath,
    bool IsDirectory,
    string? Extension,
    float[]? Embedding);
