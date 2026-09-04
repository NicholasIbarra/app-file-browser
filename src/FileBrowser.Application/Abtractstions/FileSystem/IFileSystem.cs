namespace FileBrowser.Application.Abtractstions.FileSystem;

public interface IFileSystem
{
    Task<DirectoryContents> GetDirectoryContentsAsync(
        string path,
        CancellationToken cancellationToken = default);

    Task<FileItem?> GetFileAsync(
        string path,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(
        string path,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string path,
        CancellationToken cancellationToken = default);
}
