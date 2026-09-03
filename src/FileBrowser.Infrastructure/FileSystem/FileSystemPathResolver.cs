using FileBrowser.Application.Abtractstions.FileSystem;
using Microsoft.Extensions.Options;

namespace FileBrowser.Infrastructure.FileSystem;

public sealed class FileSystemPathResolver : IFileSystemPathResolver
{
    private readonly string _rootPath;
    private readonly string _rootPathWithSeparator;

    public FileSystemPathResolver(IOptions<FileBrowserOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Value.HomeDirectory))
        {
            throw new InvalidOperationException(
                "A home directory must be configured.");
        }

        _rootPath = Path.GetFullPath(options.Value.HomeDirectory);

        _rootPathWithSeparator = _rootPath.EndsWith(
            Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;
    }

    public string Resolve(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == "/")
        {
            return _rootPath;
        }

        var relativePath = path
            .Replace('/', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);

        var physicalPath = Path.GetFullPath(
            Path.Combine(_rootPath, relativePath));

        if (!IsWithinRoot(physicalPath))
        {
            throw new UnauthorizedAccessException(
                "The requested path is outside the configured home directory.");
        }

        return physicalPath;
    }

    public string ToRelativePath(string physicalPath)
    {
        var fullPath = Path.GetFullPath(physicalPath);

        if (!IsWithinRoot(fullPath))
        {
            throw new UnauthorizedAccessException(
                "The path is outside the configured home directory.");
        }

        if (string.Equals(
                fullPath,
                _rootPath,
                StringComparison.OrdinalIgnoreCase))
        {
            return "/";
        }

        var relativePath = Path.GetRelativePath(
            _rootPath,
            fullPath);

        // Always expose URL-style paths to the client,
        // regardless of whether the server is Windows or Linux.
        return "/" + relativePath.Replace(
            Path.DirectorySeparatorChar,
            '/');
    }

    private bool IsWithinRoot(string path)
    {
        return string.Equals(
                   path,
                   _rootPath,
                   StringComparison.OrdinalIgnoreCase)
               ||
               path.StartsWith(
                   _rootPathWithSeparator,
                   StringComparison.OrdinalIgnoreCase);
    }
}