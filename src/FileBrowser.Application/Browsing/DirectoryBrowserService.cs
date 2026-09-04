using FileBrowser.Application.Abtractstions.FileSystem;
using FileBrowser.Application.FileChanges;
using MediatR;

namespace FileBrowser.Application.Browsing;

public class DirectoryBrowserService : IDirectoryBrowserService
{
    private readonly IFileSystem _fileSystem;
    private readonly IPublisher _publisher;

    public DirectoryBrowserService(IFileSystem fileSystem, IPublisher publisher)
    {
        _fileSystem = fileSystem;
        _publisher = publisher;
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

    public async Task UploadAsync(
        string path,
        Stream content,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);

        await _fileSystem.UploadAsync(path, content, overwrite, cancellationToken);
        await _publisher.Publish(new FileCreatedEvent(path), cancellationToken);
    }

    public async Task DeleteAsync(
        string path,
        bool recursive = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        await _fileSystem.DeleteAsync(path, recursive, cancellationToken);
        await _publisher.Publish(new FileDeletedEvent(path), cancellationToken);
    }

    public async Task MoveAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        await _fileSystem.MoveAsync(
            sourcePath,
            destinationPath,
            overwrite,
            cancellationToken);

        await _publisher.Publish(
            new FileMovedEvent(sourcePath, destinationPath),
            cancellationToken);
    }

    public async Task CopyAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        await _fileSystem.CopyAsync(
            sourcePath,
            destinationPath,
            overwrite,
            cancellationToken);

        await _publisher.Publish(new FileCopiedEvent(destinationPath), cancellationToken);
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
