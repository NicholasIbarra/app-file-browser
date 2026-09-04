using FileBrowser.Application.Abtractstions.FileSystem;

namespace FileBrowser.Infrastructure.FileSystem;

public class LocalFileSystem : IFileSystem
{
    private readonly IFileSystemPathResolver _pathResolver;

    public LocalFileSystem(IFileSystemPathResolver pathResolver)
    {
        _pathResolver = pathResolver;
    }

    /// <summary>
    /// Gets the contents of a directory.
    /// </summary>
    /// <param name="path">The path of the directory.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the directory contents.</returns>
    public Task<DirectoryContents> GetDirectoryContentsAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var physicalPath = _pathResolver.Resolve(path);

        if (!Directory.Exists(physicalPath))
        {
            return Task.FromResult(new DirectoryContents(path, []));
        }

        var directory = new DirectoryInfo(physicalPath);

        var entries = new List<FileItem>();

        foreach (var entry in directory.EnumerateFileSystemInfos())
        {
            cancellationToken.ThrowIfCancellationRequested();

            entries.Add(Map(entry));
        }

        var result = new DirectoryContents(
            _pathResolver.ToRelativePath(directory.FullName),
            entries);

        return Task.FromResult(result);
    }

    /// <summary>
    /// Gets a file or directory entry by its path.
    /// </summary>
    /// <param name="path">The path of the file or directory.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the file or directory entry, or null if not found.</returns>
    public Task<FileItem?> GetFileAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var physicalPath = _pathResolver.Resolve(path);

        FileSystemInfo? entry = null;

        if (Directory.Exists(physicalPath))
        {
            entry = new DirectoryInfo(physicalPath);
        }
        else if (File.Exists(physicalPath))
        {
            entry = new FileInfo(physicalPath);
        }

        return Task.FromResult(
            entry is null
                ? null
                : Map(entry));
    }

    public Task<Stream> OpenReadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var physicalPath = _pathResolver.Resolve(path);

        if (!File.Exists(physicalPath))
        {
            throw new FileNotFoundException(
                $"File '{path}' was not found.",
                path);
        }

        Stream stream = new FileStream(
            physicalPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan);

        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var physicalPath = _pathResolver.Resolve(path);

        return Task.FromResult(
            File.Exists(physicalPath) ||
            Directory.Exists(physicalPath));
    }

    private FileItem Map(FileSystemInfo entry)
    {
        return entry switch
        {
            FileInfo file => new FileItem(
                Name: file.Name,
                Path: _pathResolver.ToRelativePath(file.FullName),
                Type: FileSystemEntryType.File,
                Size: file.Length,
                LastModified: file.LastWriteTimeUtc),

            DirectoryInfo directory => new FileItem(
                Name: directory.Name,
                Path: _pathResolver.ToRelativePath(directory.FullName),
                Type: FileSystemEntryType.Directory,
                Size: null,
                LastModified: directory.LastWriteTimeUtc),

            _ => throw new NotSupportedException(
                $"Unsupported filesystem entry '{entry.GetType().Name}'.")
        };
    }

}
