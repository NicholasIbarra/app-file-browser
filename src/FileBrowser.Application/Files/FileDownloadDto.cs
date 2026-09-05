namespace FileBrowser.Application.Files;

public sealed record FileDownloadDto(
    string FileName,
    Stream Content);
