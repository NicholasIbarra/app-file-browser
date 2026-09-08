namespace FileBrowser.Application.Files;

public interface IFileService
{
    Task<FileDownloadDto> DownloadAsync(string path, CancellationToken cancellationToken = default);
    Task<string> UploadAsync(string path, Stream content, bool overwrite = false, CancellationToken cancellationToken = default);
    Task DeleteAsync(string path, bool recursive = false, CancellationToken cancellationToken = default);
    Task MoveAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default);
    Task CopyAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default);

    Task<string> RenameAsync(string sourcePath, string newName, bool overwrite = false, CancellationToken cancellationToken = default);
}
