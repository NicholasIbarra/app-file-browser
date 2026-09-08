using FileBrowser.Application.Abtractstions.BackgroundJobs;
using FileBrowser.Application.Abtractstions.FileSystem;
using FileBrowser.Application.Files.BackgroundJobs;
using FileBrowser.Application.Files.Events;
using MediatR;

namespace FileBrowser.Application.Files;

public  class FileService : IFileService
{
    private readonly IFileSystem _fileSystem;

    private readonly IPublisher _publisher;

    private readonly IBackgroundJobManager _backgroundJobs;

    public FileService(IFileSystem fileSystem, IPublisher publisher, IBackgroundJobManager backgroundJobs)
    {
        _fileSystem = fileSystem;
        _publisher = publisher;
        _backgroundJobs = backgroundJobs;
    }

    public async Task<FileDownloadDto> DownloadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var file = await _fileSystem.GetFileAsync(path, cancellationToken);

        if (file is null || file.Type != FileSystemEntryType.File)
        {
            throw new FileNotFoundException($"File '{path}' was not found.", path);
        }

        var content = await _fileSystem.OpenReadAsync(path, cancellationToken);

        return new FileDownloadDto(file.Name, content);
    }

    public async Task<string> UploadAsync(
        string path,
        Stream content,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(content);

        // Persist bytes rather than the stream, which is disposed after the request.


        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = buffer.ToArray();

        return _backgroundJobs.Enqueue<FileUploadJob>(job =>
            job.ExecuteAsync(path, bytes, overwrite, CancellationToken.None));
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

    public async Task<string> RenameAsync(string sourcePath, string newName, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(newName);

        return _backgroundJobs.Enqueue<FileRenameJob>(job =>
            job.ExecuteAsync(sourcePath, newName, overwrite, CancellationToken.None));
    }
}
