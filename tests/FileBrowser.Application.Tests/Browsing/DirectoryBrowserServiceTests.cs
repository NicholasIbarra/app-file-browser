using FileBrowser.Application.Abtractstions.FileSystem;
using FileBrowser.Application.Browsing;
using FileBrowser.Application.FileChanges;
using MediatR;
using NSubstitute;

namespace FileBrowser.Application.Tests.Browsing;

public sealed class DirectoryBrowserServiceTests
{
    private readonly IFileSystem _fileSystem;
    private readonly IPublisher _publisher;
    private readonly DirectoryBrowserService _sut;

    public DirectoryBrowserServiceTests()
    {
        _fileSystem = Substitute.For<IFileSystem>();
        _publisher = Substitute.For<IPublisher>();
        _sut = new DirectoryBrowserService(_fileSystem, _publisher);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetDirectoryContentsAsync_NullOrWhitespacePath_RequestsRootFromFileSystem(
        string? path)
    {
        _fileSystem
            .GetDirectoryContentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DirectoryContents("/", []));

        await _sut.GetDirectoryContentsAsync(path);

        await _fileSystem
            .Received(1)
            .GetDirectoryContentsAsync("/", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_NonEmptyPath_PassesPathThroughUnchanged()
    {
        const string requestedPath = "/Documents/Reports";

        _fileSystem
            .GetDirectoryContentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DirectoryContents(requestedPath, []));

        await _sut.GetDirectoryContentsAsync(requestedPath);

        await _fileSystem
            .Received(1)
            .GetDirectoryContentsAsync(requestedPath, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_PassesCancellationTokenToFileSystem()
    {
        using var cts = new CancellationTokenSource();

        _fileSystem
            .GetDirectoryContentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DirectoryContents("/", []));

        await _sut.GetDirectoryContentsAsync("/", cts.Token);

        await _fileSystem
            .Received(1)
            .GetDirectoryContentsAsync("/", cts.Token);
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_ReturnsPathFromFileSystem()
    {
        _fileSystem
            .GetDirectoryContentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DirectoryContents("/Documents", []));

        var result = await _sut.GetDirectoryContentsAsync("/Documents");

        Assert.Equal("/Documents", result.Path);
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_OrdersDirectoriesBeforeFiles()
    {
        var lastModified = DateTimeOffset.UtcNow;

        var file = new FileItem(
            "afile.txt", "/afile.txt", FileSystemEntryType.File, 10, lastModified);
        var directory = new FileItem(
            "zdirectory", "/zdirectory", FileSystemEntryType.Directory, null, lastModified);

        _fileSystem
            .GetDirectoryContentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DirectoryContents("/", [file, directory]));

        var result = await _sut.GetDirectoryContentsAsync("/");

        Assert.Equal(
            ["zdirectory", "afile.txt"],
            result.Entries.Select(e => e.Name));
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_OrdersEntriesOfSameTypeByNameCaseInsensitive()
    {
        var lastModified = DateTimeOffset.UtcNow;

        var beta = new FileItem(
            "beta", "/beta", FileSystemEntryType.Directory, null, lastModified);
        var alpha = new FileItem(
            "Alpha", "/Alpha", FileSystemEntryType.Directory, null, lastModified);

        _fileSystem
            .GetDirectoryContentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DirectoryContents("/", [beta, alpha]));

        var result = await _sut.GetDirectoryContentsAsync("/");

        Assert.Equal(
            ["Alpha", "beta"],
            result.Entries.Select(e => e.Name));
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_MapsEntryFieldsToDto()
    {
        var lastModified = DateTimeOffset.UtcNow;
        var entry = new FileItem(
            "report.pdf", "/Documents/report.pdf", FileSystemEntryType.File, 2048, lastModified);

        _fileSystem
            .GetDirectoryContentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DirectoryContents("/Documents", [entry]));

        var result = await _sut.GetDirectoryContentsAsync("/Documents");

        var mapped = Assert.Single(result.Entries);
        Assert.Equal(entry.Name, mapped.Name);
        Assert.Equal(entry.Path, mapped.Path);
        Assert.Equal(entry.Type, mapped.Type);
        Assert.Equal(entry.Size, mapped.Size);
        Assert.Equal(entry.LastModified, mapped.LastModified);
    }

    [Fact]
    public async Task GetDirectoryContentsAsync_NoEntries_ReturnsEmptyList()
    {
        _fileSystem
            .GetDirectoryContentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DirectoryContents("/", []));

        var result = await _sut.GetDirectoryContentsAsync("/");

        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task UploadAsync_ForwardsRequestToFileSystem()
    {
        await using var content = new MemoryStream("content"u8.ToArray());
        using var cts = new CancellationTokenSource();

        await _sut.UploadAsync("/notes.txt", content, overwrite: true, cts.Token);

        await _fileSystem.Received(1).UploadAsync("/notes.txt", content, true, cts.Token);
    }

    [Fact]
    public async Task DeleteAsync_ForwardsRequestToFileSystem()
    {
        using var cts = new CancellationTokenSource();

        await _sut.DeleteAsync("/Documents", recursive: true, cts.Token);

        await _fileSystem.Received(1).DeleteAsync("/Documents", true, cts.Token);
    }

    [Fact]
    public async Task DeleteAsync_AfterDeletion_PublishesFileDeletedEvent()
    {
        using var cts = new CancellationTokenSource();

        await _sut.DeleteAsync("/Documents/report.pdf", cancellationToken: cts.Token);

        await _publisher.Received(1).Publish(
            Arg.Is<FileDeletedEvent>(notification =>
                notification.Path == "/Documents/report.pdf"),
            cts.Token);
    }

    [Fact]
    public async Task DeleteAsync_WhenDeletionFails_DoesNotPublishFileDeletedEvent()
    {
        _fileSystem
            .DeleteAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new IOException("Delete failed.")));

        await Assert.ThrowsAsync<IOException>(() => _sut.DeleteAsync("/Documents/report.pdf"));

        await _publisher.DidNotReceive().Publish(
            Arg.Any<FileDeletedEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveAsync_ForwardsRequestToFileSystem()
    {
        using var cts = new CancellationTokenSource();

        await _sut.MoveAsync("/source", "/destination", overwrite: true, cts.Token);

        await _fileSystem.Received(1).MoveAsync("/source", "/destination", true, cts.Token);
    }

    [Fact]
    public async Task CopyAsync_ForwardsRequestToFileSystem()
    {
        using var cts = new CancellationTokenSource();

        await _sut.CopyAsync("/source", "/destination", overwrite: true, cts.Token);

        await _fileSystem.Received(1).CopyAsync("/source", "/destination", true, cts.Token);
    }
}
