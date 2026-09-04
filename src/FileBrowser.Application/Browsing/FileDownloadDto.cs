namespace FileBrowser.Application.Browsing;

public sealed record FileDownloadDto(
    string FileName,
    Stream Content);
