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
            throw new DirectoryNotFoundException(
                $"Directory '{path}' was not found.");
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
    /// Gets all file and directory entries in the specified path.
    /// </summary>
    /// <param name="path">The path of the directory.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the file and directory entries.</returns>

    public Task<IReadOnlyList<FileItem>> GetAllDirectoryContentsAsync(
        string? path,
        CancellationToken cancellationToken = default)
    {
        var results = new List<FileItem>();

        var root = new DirectoryInfo(_pathResolver.Resolve(path));

        if (!root.Exists)
        {
            return Task.FromResult<IReadOnlyList<FileItem>>(Array.Empty<FileItem>());
        }

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false,
            AttributesToSkip = FileAttributes.ReparsePoint // Avoid following directory junctions / symlinks
        };

        foreach (var entry in root.EnumerateFileSystemInfos("*", options))
        {
            cancellationToken.ThrowIfCancellationRequested();

            results.Add(Map(entry));
        }

        return Task.FromResult<IReadOnlyList<FileItem>>(results);
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

    public async Task UploadAsync(
        string path,
        Stream content,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();

        var physicalPath = _pathResolver.Resolve(path);
        EnsureParentDirectoryExists(physicalPath);

        if (Directory.Exists(physicalPath))
        {
            throw new IOException($"A directory already exists at '{path}'.");
        }

        await using var destination = new FileStream(
            physicalPath,
            overwrite ? FileMode.Create : FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.Asynchronous);

        await content.CopyToAsync(destination, cancellationToken);
    }

    public Task DeleteAsync(
        string path,
        bool recursive = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var physicalPath = _pathResolver.Resolve(path);

        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }
        else if (Directory.Exists(physicalPath))
        {
            EnsureNotRoot(physicalPath);
            Directory.Delete(physicalPath, recursive);
        }
        else
        {
            throw new FileNotFoundException($"File or directory '{path}' was not found.", path);
        }

        return Task.CompletedTask;
    }

    public Task MoveAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var source = _pathResolver.Resolve(sourcePath);
        var destination = _pathResolver.Resolve(destinationPath);
        EnsureNotRoot(source);
        EnsureParentDirectoryExists(destination);

        if (File.Exists(source))
        {
            if (Directory.Exists(destination))
            {
                throw new IOException($"A directory already exists at '{destinationPath}'.");
            }

            File.Move(source, destination, overwrite);
        }
        else if (Directory.Exists(source))
        {
            if (File.Exists(destination) || Directory.Exists(destination))
            {
                throw new IOException($"An entry already exists at '{destinationPath}'.");
            }

            Directory.Move(source, destination);
        }
        else
        {
            throw new FileNotFoundException($"File or directory '{sourcePath}' was not found.", sourcePath);
        }

        return Task.CompletedTask;
    }

    public async Task CopyAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var source = _pathResolver.Resolve(sourcePath);
        var destination = _pathResolver.Resolve(destinationPath);
        EnsureParentDirectoryExists(destination);

        if (File.Exists(source))
        {
            if (Directory.Exists(destination))
            {
                throw new IOException($"A directory already exists at '{destinationPath}'.");
            }

            await CopyFileAsync(source, destination, overwrite, cancellationToken);
        }
        else if (Directory.Exists(source))
        {
            if (File.Exists(destination) || Directory.Exists(destination))
            {
                throw new IOException($"An entry already exists at '{destinationPath}'.");
            }

            EnsureDestinationIsNotWithinSource(source, destination);
            await CopyDirectoryAsync(source, destination, cancellationToken);
        }
        else
        {
            throw new FileNotFoundException($"File or directory '{sourcePath}' was not found.", sourcePath);
        }
    }

    private static async Task CopyDirectoryAsync(
        string source,
        string destination,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destination);

        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            await CopyFileAsync(file, target, overwrite: false, cancellationToken);
        }
    }

    private static async Task CopyFileAsync(
        string source,
        string destination,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        await using var sourceStream = new FileStream(
            source, FileMode.Open, FileAccess.Read, FileShare.Read, 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var destinationStream = new FileStream(
            destination, overwrite ? FileMode.Create : FileMode.CreateNew,
            FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        await sourceStream.CopyToAsync(destinationStream, cancellationToken);
    }

    private static void EnsureParentDirectoryExists(string path)
    {
        var parent = Path.GetDirectoryName(path);
        if (parent is null || !Directory.Exists(parent))
        {
            throw new DirectoryNotFoundException($"The destination directory for '{path}' was not found.");
        }
    }

    private void EnsureNotRoot(string path)
    {
        if (_pathResolver.ToRelativePath(path) == "/")
        {
            throw new InvalidOperationException("The file system root cannot be modified.");
        }
    }

    private static void EnsureDestinationIsNotWithinSource(string source, string destination)
    {
        var sourcePrefix = source.EndsWith(Path.DirectorySeparatorChar)
            ? source
            : source + Path.DirectorySeparatorChar;

        if (destination.StartsWith(sourcePrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException("A directory cannot be copied into itself.");
        }
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
                ChildCount: null,
                LastModified: file.LastWriteTimeUtc),

            DirectoryInfo directory => new FileItem(
                Name: directory.Name,
                Path: _pathResolver.ToRelativePath(directory.FullName),
                Type: FileSystemEntryType.Directory,
                Size: null,
                ChildCount: GetChildCount(_pathResolver.ToRelativePath(directory.FullName)),
                LastModified: directory.LastWriteTimeUtc),

            _ => throw new NotSupportedException(
                $"Unsupported filesystem entry '{entry.GetType().Name}'.")
        };
    }

    private int? GetChildCount(string path)
    {
        var root = _pathResolver.Resolve(path);
        var directoryInfo = new DirectoryInfo(root);

        return directoryInfo.Exists
            ? directoryInfo.EnumerateFileSystemInfos().Count()
            : null;
    }
}
