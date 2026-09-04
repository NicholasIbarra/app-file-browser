using FileBrowser.Application.Abtractstions.FileSystem;
using FileBrowser.Infrastructure.FileSystem;
using FileBrowser.Infrastructure.Indexing;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FileBrowser.Infrastructure.Tests.Indexing;

public sealed class FileSearchIndexTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(
        Path.GetTempPath(),
        $"file-browser-index-tests-{Guid.NewGuid():N}");

    [Theory]
    [InlineData("/Documents/report.pdf", "Documents/report.pdf")]
    [InlineData("\\Documents\\report.pdf", "Documents/report.pdf")]
    [InlineData("//Documents/report.pdf//", "Documents/report.pdf")]
    [InlineData("report.pdf", "report.pdf")]
    public void NormalizeRelativePath_NormalizesDirectorySeparatorsAndOuterSlashes(
        string path,
        string expected)
    {
        var result = FileSearchIndex.NormalizeRelativePath(path);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Documents", "Documents", true)]
    [InlineData("documents", "Documents", true)]
    [InlineData("Documents/report.pdf", "Documents", true)]
    [InlineData("Documents\\Reports\\report.pdf", "Documents", true)]
    [InlineData("Documents-Archive/report.pdf", "Documents", false)]
    [InlineData("Document", "Documents", false)]
    [InlineData("Other/report.pdf", "Documents", false)]
    public void IsPathOrDescendant_ReturnsExpectedResult(
        string candidatePath,
        string parentPath,
        bool expected)
    {
        var result = FileSearchIndex.IsPathOrDescendant(candidatePath, parentPath);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task RebuildAsync_IndexesEntriesReturnedByFileSystemProvider()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem
            .GetAllDirectoryContentsAsync("/", Arg.Any<CancellationToken>())
            .Returns([
                new FileItem(
                    "report.pdf",
                    "/Documents/report.pdf",
                    FileSystemEntryType.File,
                    42,
                    null,
                    DateTimeOffset.UtcNow)
            ]);
        using var sut = new FileSearchIndex(fileSystem);

        await sut.RebuildAsync(CancellationToken.None);

        var entry = Assert.Single(sut.Snapshot);
        Assert.Equal("Documents/report.pdf", entry.RelativePath);
        Assert.Equal("/Documents/report.pdf", entry.FullPath);
        await fileSystem.Received(1)
            .GetAllDirectoryContentsAsync("/", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddOrUpdateAsync_ProviderDirectory_IndexesEntryAndDescendants()
    {
        var fileSystem = Substitute.For<IFileSystem>();
        var modified = DateTimeOffset.UtcNow;
        fileSystem
            .GetFileAsync("/Documents", Arg.Any<CancellationToken>())
            .Returns(new FileItem(
                "Documents", "/Documents", FileSystemEntryType.Directory, null, null, modified));
        fileSystem
            .GetAllDirectoryContentsAsync("/Documents", Arg.Any<CancellationToken>())
            .Returns([
                new FileItem(
                    "report.pdf",
                    "/Documents/report.pdf",
                    FileSystemEntryType.File,
                    42,
                    null,
                    modified)
            ]);
        using var sut = new FileSearchIndex(fileSystem);

        await sut.AddOrUpdateAsync("/Documents", CancellationToken.None);

        Assert.Collection(
            sut.Snapshot,
            entry => Assert.Equal("Documents", entry.RelativePath),
            entry => Assert.Equal("Documents/report.pdf", entry.RelativePath));
    }

    [Fact]
    public async Task AddOrUpdateAsync_NewFile_AddsEntry()
    {
        Directory.CreateDirectory(_rootPath);
        var filePath = Path.Combine(_rootPath, "uploaded.txt");
        await File.WriteAllTextAsync(filePath, "uploaded");
        using var sut = CreateIndex();

        await sut.AddOrUpdateAsync("/uploaded.txt", CancellationToken.None);

        var entry = Assert.Single(sut.Snapshot);
        Assert.Equal("uploaded.txt", entry.Name);
        Assert.Equal("uploaded.txt", entry.RelativePath);
        Assert.False(entry.IsDirectory);
        Assert.Equal(".txt", entry.Extension);
    }

    [Fact]
    public async Task AddOrUpdateAsync_ExistingFile_ReplacesEntryWithoutDuplicate()
    {
        Directory.CreateDirectory(_rootPath);
        var filePath = Path.Combine(_rootPath, "uploaded.txt");
        await File.WriteAllTextAsync(filePath, "first version");
        using var sut = CreateIndex();
        await sut.RebuildAsync(CancellationToken.None);
        await File.WriteAllTextAsync(filePath, "second version");

        await sut.AddOrUpdateAsync("/uploaded.txt", CancellationToken.None);

        Assert.Single(sut.Snapshot, entry => entry.Name == "uploaded.txt");
    }

    [Fact]
    public async Task AddOrUpdateAsync_Directory_AddsDirectoryAndDescendants()
    {
        var directory = Directory.CreateDirectory(Path.Combine(_rootPath, "Copied"));
        await File.WriteAllTextAsync(Path.Combine(directory.FullName, "copied.txt"), "copied");
        using var sut = CreateIndex();

        await sut.AddOrUpdateAsync("/Copied", CancellationToken.None);

        Assert.Contains(sut.Snapshot, entry => entry.Name == "Copied" && entry.IsDirectory);
        Assert.Contains(sut.Snapshot, entry => entry.Name == "copied.txt" && !entry.IsDirectory);
    }

    [Fact]
    public async Task MoveAsync_Directory_RemovesSourceAndAddsDestinationSubtree()
    {
        var source = Directory.CreateDirectory(Path.Combine(_rootPath, "Source"));
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "moved.txt"), "moved");
        using var sut = CreateIndex();
        await sut.RebuildAsync(CancellationToken.None);
        Directory.Move(source.FullName, Path.Combine(_rootPath, "Destination"));

        await sut.MoveAsync("/Source", "/Destination", CancellationToken.None);

        Assert.DoesNotContain(
            sut.Snapshot,
            entry => entry.RelativePath.Replace('\\', '/').StartsWith("Source"));
        Assert.Contains(sut.Snapshot, entry => entry.Name == "Destination" && entry.IsDirectory);
        Assert.Contains(sut.Snapshot, entry => entry.Name == "moved.txt" && !entry.IsDirectory);
    }

    [Fact]
    public async Task RemoveAsync_FilePath_RemovesOnlyMatchingEntry()
    {
        Directory.CreateDirectory(_rootPath);
        await File.WriteAllTextAsync(Path.Combine(_rootPath, "deleted.txt"), "deleted");
        await File.WriteAllTextAsync(Path.Combine(_rootPath, "retained.txt"), "retained");
        using var sut = CreateIndex();
        await sut.RebuildAsync(CancellationToken.None);

        await sut.RemoveAsync("/deleted.txt", CancellationToken.None);

        Assert.DoesNotContain(sut.Snapshot, entry => entry.Name == "deleted.txt");
        Assert.Contains(sut.Snapshot, entry => entry.Name == "retained.txt");
    }

    [Fact]
    public async Task RemoveAsync_DirectoryPath_RemovesDirectoryAndDescendants()
    {
        var deletedDirectory = Directory.CreateDirectory(
            Path.Combine(_rootPath, "Documents", "Reports"));
        await File.WriteAllTextAsync(
            Path.Combine(deletedDirectory.FullName, "report.pdf"),
            "report");
        Directory.CreateDirectory(Path.Combine(_rootPath, "Documents-Archive"));
        using var sut = CreateIndex();
        await sut.RebuildAsync(CancellationToken.None);

        await sut.RemoveAsync("/Documents", CancellationToken.None);

        Assert.DoesNotContain(
            sut.Snapshot,
            entry => entry.RelativePath.Replace('\\', '/').StartsWith("Documents/"));
        Assert.DoesNotContain(sut.Snapshot, entry => entry.Name == "Documents");
        Assert.Contains(sut.Snapshot, entry => entry.Name == "Documents-Archive");
    }

    private FileSearchIndex CreateIndex()
    {
        var options = Options.Create(new FileBrowserOptions
        {
            HomeDirectory = _rootPath
        });
        var fileSystem = new LocalFileSystem(new FileSystemPathResolver(options));

        return new FileSearchIndex(fileSystem);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
