using FileBrowser.Application.Abtractstions.FileSystem;
using System;
using System.Collections.Generic;
using System.Text;

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
    private static FileSystemEntryDto Map(FileSystemEntry entry)
    {
        return new FileSystemEntryDto(
            entry.Name,
            entry.Path,
            entry.Type,
            entry.Size,
            entry.LastModified);
    }


}
