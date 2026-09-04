using FileBrowser.Application.Abtractstions.FileSystem;

namespace FileBrowser.Application.Browsing;

public class DirectoryBrowserService : IDirectoryBrowserService
{
    private readonly IFileSystem _fileSystem;

    public DirectoryBrowserService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<DirectoryContentsDto> GetDirectoryContentsAsync(
        string? path,
        CancellationToken cancellationToken = default)
    {
        var requestedPath = NormalizePath(path);

        var contents = await _fileSystem.GetDirectoryContentsAsync(
            requestedPath,
            cancellationToken);

        var entries = contents.Entries
            .OrderBy(i => i.Type == FileSystemEntryType.Directory ? 0 : 1)
                .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .Select(Map)
            .ToList();

        return new DirectoryContentsDto(contents.Path, entries);
    }

    public Task UploadAsync(
        string path,
        Stream content,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);
        return _fileSystem.UploadAsync(path, content, overwrite, cancellationToken);
    }

    public Task DeleteAsync(
        string path,
        bool recursive = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return _fileSystem.DeleteAsync(path, recursive, cancellationToken);
    }

    public Task MoveAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        return _fileSystem.MoveAsync(sourcePath, destinationPath, overwrite, cancellationToken);
    }

    public Task CopyAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        return _fileSystem.CopyAsync(sourcePath, destinationPath, overwrite, cancellationToken);
    }

    /// <summary>
    /// Handle the null path meant to be the root of the folder
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        return path;
    }

    private static FileSystemEntryDto Map(FileItem entry)
    {
        return new FileSystemEntryDto(
            entry.Name,
            entry.Path,
            entry.Type,
            entry.Size,
            entry.LastModified);
    }


}
