namespace FileBrowser.Application.Browsing;

public interface IDirectoryBrowserService
{
    Task<DirectoryContentsDto> GetDirectoryContentsAsync(string? path, string? sort = null, CancellationToken cancellationToken = default);
}
