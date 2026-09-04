namespace FileBrowser.Api.Models;

public sealed record FileOperationRequest(
    string SourcePath,
    string DestinationPath,
    bool Overwrite = false);
