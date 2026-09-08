namespace FileBrowser.Application.Abtractstions.FileSystem;

public interface IFileSystem
{
    Task<DirectoryContents> GetDirectoryContentsAsync(
        string path,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FileItem>> GetAllDirectoryContentsAsync(
        string? path,
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

    Task UploadAsync(
        string path,
        Stream content,
        bool overwrite = false,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string path,
        bool recursive = false,
        CancellationToken cancellationToken = default);

    Task MoveAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default);

    Task CopyAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default);

    Task RenameAsync(
        string sourcePath,
        string newName,
        bool overwrite = false,
        CancellationToken cancellationToken = default);
}
