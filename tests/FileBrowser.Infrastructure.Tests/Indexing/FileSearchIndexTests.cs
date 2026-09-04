using FileBrowser.Infrastructure.FileSystem;
using FileBrowser.Infrastructure.Indexing;
using Microsoft.Extensions.Options;

namespace FileBrowser.Infrastructure.Tests.Indexing;

public sealed class FileSearchIndexTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(
        Path.GetTempPath(),
        $"file-browser-index-tests-{Guid.NewGuid():N}");

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
        return new FileSearchIndex(Options.Create(new FileBrowserOptions
        {
            HomeDirectory = _rootPath
        }));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}
