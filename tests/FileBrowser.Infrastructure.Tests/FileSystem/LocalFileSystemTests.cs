using FileBrowser.Application.Abtractstions.FileSystem;
using FileBrowser.Infrastructure.FileSystem;
using Microsoft.Extensions.Options;

namespace FileBrowser.Infrastructure.Tests.FileSystem;

public sealed class LocalFileSystemTests : IDisposable
{
    private readonly string _root;
    private readonly LocalFileSystem _sut;

    public LocalFileSystemTests()
    {
        _root = Path.Combine(
            Path.GetTempPath(),
            $"file-browser-tests-{Guid.NewGuid():N}");

        Directory.CreateDirectory(_root);

        var options = Options.Create(new FileBrowserOptions
        {
            HomeDirectory = _root
        });

        var resolver = new FileSystemPathResolver(options);

        _sut = new LocalFileSystem(resolver);
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_ReturnsFilesAndDirectories()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Documents"));
        await File.WriteAllTextAsync(
            Path.Combine(_root, "notes.txt"),
            "hello");

        var result = await _sut.GetDirectoryContentsAsync("/");

        Assert.Equal("/", result.Path);

        Assert.Contains(
            result.Entries,
            x => x.Name == "Documents"
                 && x.Type == FileSystemEntryType.Directory);

        Assert.Contains(
            result.Entries,
            x => x.Name == "notes.txt"
                 && x.Type == FileSystemEntryType.File);
    }

    [Fact]
    public async Task GetEntryAsync_ReturnsFileMetadata()
    {
        var filePath = Path.Combine(_root, "notes.txt");
        await File.WriteAllTextAsync(filePath, "hello");

        var result = await _sut.GetFileAsync("/notes.txt");

        Assert.NotNull(result);
        Assert.Equal("notes.txt", result.Name);
        Assert.Equal("/notes.txt", result.Path);
        Assert.Equal(FileSystemEntryType.File, result.Type);
        Assert.Equal(5, result.Size);
    }

    [Fact]
    public async Task GetEntryAsync_ReturnsNull_WhenEntryDoesNotExist()
    {
        var result = await _sut.GetFileAsync("/missing.txt");

        Assert.Null(result);
    }

    [Fact]
    public async Task OpenReadAsync_ReturnsFileContents()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_root, "notes.txt"),
            "hello");

        await using var stream = await _sut.OpenReadAsync("/notes.txt");
        using var reader = new StreamReader(stream);

        var content = await reader.ReadToEndAsync();

        Assert.Equal("hello", content);
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_Throws_WhenDirectoryDoesNotExist()
    {
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _sut.GetDirectoryContentsAsync("/missing"));
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_ForExistingFile()
    {
        await File.WriteAllTextAsync(
            Path.Combine(_root, "notes.txt"),
            "hello");

        var exists = await _sut.ExistsAsync("/notes.txt");

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_ForMissingEntry()
    {
        var exists = await _sut.ExistsAsync("/missing.txt");

        Assert.False(exists);
    }

    [Fact]
    public async Task UploadAsync_WritesContent_AndHonorsOverwrite()
    {
        await using var content = new MemoryStream("hello"u8.ToArray());
        await _sut.UploadAsync("/notes.txt", content);

        Assert.Equal("hello", await File.ReadAllTextAsync(Path.Combine(_root, "notes.txt")));

        await using var replacement = new MemoryStream("updated"u8.ToArray());
        await _sut.UploadAsync("/notes.txt", replacement, overwrite: true);

        Assert.Equal("updated", await File.ReadAllTextAsync(Path.Combine(_root, "notes.txt")));
    }

    [Fact]
    public async Task DeleteAsync_DeletesFilesAndDirectories()
    {
        await File.WriteAllTextAsync(Path.Combine(_root, "notes.txt"), "hello");
        var directory = Directory.CreateDirectory(Path.Combine(_root, "Documents"));
        await File.WriteAllTextAsync(Path.Combine(directory.FullName, "nested.txt"), "nested");

        await _sut.DeleteAsync("/notes.txt");
        await _sut.DeleteAsync("/Documents", recursive: true);

        Assert.False(File.Exists(Path.Combine(_root, "notes.txt")));
        Assert.False(Directory.Exists(directory.FullName));
    }

    [Fact]
    public async Task MoveAsync_MovesFilesAndDirectories()
    {
        await File.WriteAllTextAsync(Path.Combine(_root, "notes.txt"), "hello");
        Directory.CreateDirectory(Path.Combine(_root, "Documents"));

        await _sut.MoveAsync("/notes.txt", "/renamed.txt");
        await _sut.MoveAsync("/Documents", "/Archive");

        Assert.Equal("hello", await File.ReadAllTextAsync(Path.Combine(_root, "renamed.txt")));
        Assert.True(Directory.Exists(Path.Combine(_root, "Archive")));
    }

    [Fact]
    public async Task CopyAsync_CopiesFilesAndDirectoryTrees()
    {
        await File.WriteAllTextAsync(Path.Combine(_root, "notes.txt"), "hello");
        var directory = Directory.CreateDirectory(Path.Combine(_root, "Documents", "Nested"));
        await File.WriteAllTextAsync(Path.Combine(directory.FullName, "file.txt"), "nested");

        await _sut.CopyAsync("/notes.txt", "/notes-copy.txt");
        await _sut.CopyAsync("/Documents", "/Documents-copy");

        Assert.Equal("hello", await File.ReadAllTextAsync(Path.Combine(_root, "notes-copy.txt")));
        Assert.Equal(
            "nested",
            await File.ReadAllTextAsync(Path.Combine(_root, "Documents-copy", "Nested", "file.txt")));
    }

    [Fact]
    public async Task MutatingOperations_RejectPathTraversal()
    {
        await using var content = new MemoryStream("hello"u8.ToArray());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.UploadAsync("../../outside.txt", content));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.DeleteAsync("../../outside.txt"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.MoveAsync("/source", "../../outside"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.CopyAsync("/source", "../../outside"));
    }

    [Fact]
    public async Task Operations_RejectPathTraversal()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.GetDirectoryContentsAsync("../../"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
