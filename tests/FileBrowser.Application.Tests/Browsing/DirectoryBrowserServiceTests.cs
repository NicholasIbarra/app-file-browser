using System.Linq.Expressions;
using FileBrowser.Application.Abtractstions.BackgroundJobs;
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
    private readonly IBackgroundJobManager _backgroundJobs = Substitute.For<IBackgroundJobManager>();
    private readonly DirectoryBrowserService _sut;

    public DirectoryBrowserServiceTests()
    {
        _fileSystem = Substitute.For<IFileSystem>();
        _publisher = Substitute.For<IPublisher>();
        _sut = new DirectoryBrowserService(_fileSystem, _publisher, _backgroundJobs);
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
            "afile.txt", "/afile.txt", FileSystemEntryType.File, 10, null, lastModified);
        var directory = new FileItem(
            "zdirectory", "/zdirectory", FileSystemEntryType.Directory, null, 3, lastModified);

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
            "beta", "/beta", FileSystemEntryType.Directory, null, 0, lastModified);
        var alpha = new FileItem(
            "Alpha", "/Alpha", FileSystemEntryType.Directory, null, 0, lastModified);

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
            "Documents", "/Documents", FileSystemEntryType.Directory, null, 4, lastModified);

        _fileSystem
            .GetDirectoryContentsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new DirectoryContents("/Documents", [entry]));

        var result = await _sut.GetDirectoryContentsAsync("/Documents");

        var mapped = Assert.Single(result.Entries);
        Assert.Equal(entry.Name, mapped.Name);
        Assert.Equal(entry.Path, mapped.Path);
        Assert.Equal(entry.Type, mapped.Type);
        Assert.Equal(entry.Size, mapped.Size);
        Assert.Equal(entry.ChildCount, mapped.ChildCount);
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
    public async Task DownloadAsync_ExistingFile_ReturnsFileNameAndContentStream()
    {
        var file = new FileItem(
            "report.pdf",
            "/Documents/report.pdf",
            FileSystemEntryType.File,
            1024,
            null,
            DateTimeOffset.UtcNow);
        var content = new MemoryStream("report"u8.ToArray());
        _fileSystem
            .GetFileAsync("/Documents/report.pdf", Arg.Any<CancellationToken>())
            .Returns(file);
        _fileSystem
            .OpenReadAsync("/Documents/report.pdf", Arg.Any<CancellationToken>())
            .Returns(content);

        var result = await _sut.DownloadAsync("/Documents/report.pdf");

        Assert.Equal("report.pdf", result.FileName);
        Assert.Same(content, result.Content);
    }

    [Fact]
    public async Task DownloadAsync_PassesCancellationTokenToFileSystem()
    {
        using var cts = new CancellationTokenSource();
        var file = new FileItem(
            "notes.txt",
            "/notes.txt",
            FileSystemEntryType.File,
            5,
            null,
            DateTimeOffset.UtcNow);
        var content = new MemoryStream("notes"u8.ToArray());
        _fileSystem.GetFileAsync("/notes.txt", cts.Token).Returns(file);
        _fileSystem.OpenReadAsync("/notes.txt", cts.Token).Returns(content);

        await _sut.DownloadAsync("/notes.txt", cts.Token);

        await _fileSystem.Received(1).GetFileAsync("/notes.txt", cts.Token);
        await _fileSystem.Received(1).OpenReadAsync("/notes.txt", cts.Token);
    }

    [Fact]
    public async Task DownloadAsync_MissingFile_ThrowsAndDoesNotOpenStream()
    {
        _fileSystem
            .GetFileAsync("/missing.txt", Arg.Any<CancellationToken>())
            .Returns((FileItem?)null);

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _sut.DownloadAsync("/missing.txt"));

        await _fileSystem.DidNotReceive().OpenReadAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DownloadAsync_Directory_ThrowsAndDoesNotOpenStream()
    {
        var directory = new FileItem(
            "Documents",
            "/Documents",
            FileSystemEntryType.Directory,
            null,
            null,
            DateTimeOffset.UtcNow);
        _fileSystem
            .GetFileAsync("/Documents", Arg.Any<CancellationToken>())
            .Returns(directory);

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _sut.DownloadAsync("/Documents"));

        await _fileSystem.DidNotReceive().OpenReadAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_ReturnsJobId_AndRunsAfterRequestStreamIsDisposed()
    {
        Expression<Func<FileUploadJob, Task>>? queuedJob = null;
        _backgroundJobs.Enqueue(Arg.Any<Expression<Func<FileUploadJob, Task>>>())
            .Returns(call =>
            {
                queuedJob = call.Arg<Expression<Func<FileUploadJob, Task>>>();
                return "job-123";
            });
        using var cts = new CancellationTokenSource();
        using (var content = new MemoryStream("content"u8.ToArray()))
        {
            Assert.Equal("job-123", await _sut.UploadAsync("/notes.txt", content, true, cts.Token));
        }
        cts.Cancel();
        Assert.Empty(_fileSystem.ReceivedCalls());
        Assert.Empty(_publisher.ReceivedCalls());

        byte[]? uploaded = null;
        _fileSystem.UploadAsync("/notes.txt", Arg.Any<Stream>(), true, CancellationToken.None)
            .Returns(async call =>
            {
                using var buffer = new MemoryStream();
                await call.Arg<Stream>().CopyToAsync(buffer);
                uploaded = buffer.ToArray();
            });
        Assert.NotNull(queuedJob);
        await queuedJob.Compile()(new FileUploadJob(_fileSystem, _publisher));

        Assert.Equal("content"u8.ToArray(), uploaded);
        await _publisher.Received(1).Publish(
            Arg.Is<FileCreatedEvent>(notification => notification.Path == "/notes.txt"),
            CancellationToken.None);
    }

    [Fact]
    public async Task UploadAsync_WhenCancelled_DoesNotEnqueue()
    {
        using var content = new MemoryStream("content"u8.ToArray());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _sut.UploadAsync("/notes.txt", content, cancellationToken: cts.Token));

        Assert.Empty(_backgroundJobs.ReceivedCalls());
    }

    [Fact]
    public async Task FileUploadJob_WhenUploadFails_DoesNotPublishFileCreatedEvent()
    {
        _fileSystem
            .UploadAsync(
                Arg.Any<string>(),
                Arg.Any<Stream>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new IOException("Upload failed.")));
        var job = new FileUploadJob(_fileSystem, _publisher);

        await Assert.ThrowsAsync<IOException>(() =>
            job.ExecuteAsync("/notes.txt", "content"u8.ToArray(), false, CancellationToken.None));

        Assert.Empty(_publisher.ReceivedCalls());
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
    public async Task MoveAsync_AfterMove_PublishesFileMovedEvent()
    {
        using var cts = new CancellationTokenSource();

        await _sut.MoveAsync("/source", "/destination", cancellationToken: cts.Token);

        await _publisher.Received(1).Publish(
            Arg.Is<FileMovedEvent>(notification =>
                notification.SourcePath == "/source"
                && notification.DestinationPath == "/destination"),
            cts.Token);
    }

    [Fact]
    public async Task MoveAsync_WhenMoveFails_DoesNotPublishFileMovedEvent()
    {
        _fileSystem
            .MoveAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new IOException("Move failed.")));

        await Assert.ThrowsAsync<IOException>(() => _sut.MoveAsync("/source", "/destination"));

        await _publisher.DidNotReceive().Publish(
            Arg.Any<FileMovedEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CopyAsync_ForwardsRequestToFileSystem()
    {
        using var cts = new CancellationTokenSource();

        await _sut.CopyAsync("/source", "/destination", overwrite: true, cts.Token);

        await _fileSystem.Received(1).CopyAsync("/source", "/destination", true, cts.Token);
    }

    [Fact]
    public async Task CopyAsync_AfterCopy_PublishesFileCopiedEvent()
    {
        using var cts = new CancellationTokenSource();

        await _sut.CopyAsync("/source", "/destination", cancellationToken: cts.Token);

        await _publisher.Received(1).Publish(
            Arg.Is<FileCopiedEvent>(notification =>
                notification.DestinationPath == "/destination"),
            cts.Token);
    }

    [Fact]
    public async Task CopyAsync_WhenCopyFails_DoesNotPublishFileCopiedEvent()
    {
        _fileSystem
            .CopyAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new IOException("Copy failed.")));

        await Assert.ThrowsAsync<IOException>(() => _sut.CopyAsync("/source", "/destination"));

        await _publisher.DidNotReceive().Publish(
            Arg.Any<FileCopiedEvent>(),
            Arg.Any<CancellationToken>());
    }
}
