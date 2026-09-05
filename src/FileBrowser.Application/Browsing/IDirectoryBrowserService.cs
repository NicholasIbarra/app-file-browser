namespace FileBrowser.Application.Browsing;

public interface IDirectoryBrowserService
{
    Task<DirectoryContentsDto> GetDirectoryContentsAsync(string? path, CancellationToken cancellationToken = default);
}
