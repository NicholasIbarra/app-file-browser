namespace FileBrowser.Application.Browsing;

public interface IDirectoryBrowserService
{
    Task<DirectoryContentsDto> GetDirectoryContentsAsync(string? path, CancellationToken cancellationToken = default);

    Task UploadAsync(string path, Stream content, bool overwrite = false, CancellationToken cancellationToken = default);
    Task DeleteAsync(string path, bool recursive = false, CancellationToken cancellationToken = default);
    Task MoveAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default);
    Task CopyAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default);
}
