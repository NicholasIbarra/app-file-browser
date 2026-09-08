using FileBrowser.Application.Abtractstions.BackgroundJobs;
using FileBrowser.Application.Abtractstions.FileSystem;
using MediatR;

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
        string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var requestedPath = NormalizePath(path);

        var contents = await _fileSystem.GetDirectoryContentsAsync(
            requestedPath,
            cancellationToken);

        var entries = SortEntries(contents.Entries, sort);

        var resultEntries = entries.Select(Map).ToList();

        return new DirectoryContentsDto(contents.Path, resultEntries);
    }

    internal IEnumerable<FileItem> SortEntries(IEnumerable<FileItem> entries, string? sort)
    {
        var result = entries.OrderBy(i => i.Type == FileSystemEntryType.Directory ? 0 : 1);

        if (string.IsNullOrWhiteSpace(sort))
        {
            return result;
        }
        return sort.ToLowerInvariant() switch
        {
            "name asc" => result.ThenBy(e => e.Name).ToList(),
            "name desc" => result.ThenByDescending(e => e.Name).ToList(),
            "size asc" => result.ThenBy(e => e.Size).ToList(),
            "size desc" => result.ThenByDescending(e => e.Size).ToList(),
            "lastmodified asc" => result.ThenBy(e => e.LastModified).ToList(),
            "lastmodified desc" => result.ThenByDescending(e => e.LastModified).ToList(),
            _ => result
        };
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
            entry.ChildCount,
            entry.LastModified);
    }
}
