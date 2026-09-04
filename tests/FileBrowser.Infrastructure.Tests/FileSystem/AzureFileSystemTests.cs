using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FileBrowser.Application.Abtractstions.FileSystem;
using FileBrowser.Infrastructure.FileSystem;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FileBrowser.Infrastructure.Tests.FileSystem;

public sealed class AzureFileSystemTests
{
    [Fact]
    public async Task GetDirectoryContentsAsync_PopulatesImmediateChildCounts()
    {
        var container = Substitute.For<BlobContainerClient>();
        container
            .GetBlobsByHierarchyAsync(
                BlobTraits.None,
                BlobStates.None,
                "/",
                "root/",
                Arg.Any<CancellationToken>())
            .Returns(Page(
                Directory("root/Documents/"),
                Directory("root/Empty/")));
        container
            .GetBlobsByHierarchyAsync(
                BlobTraits.None,
                BlobStates.None,
                "/",
                "root/Documents/",
                Arg.Any<CancellationToken>())
            .Returns(Page(
                Blob("root/Documents/"),
                Blob("root/Documents/report.pdf"),
                Directory("root/Documents/Nested/")));
        container
            .GetBlobsByHierarchyAsync(
                BlobTraits.None,
                BlobStates.None,
                "/",
                "root/Empty/",
                Arg.Any<CancellationToken>())
            .Returns(Page(Blob("root/Empty/")));

        var sut = new AzureFileSystem(
            container,
            Options.Create(new FileBrowserOptions
            {
                Azure = new AzureFileSystemOptions { Prefix = "root" }
            }));

        var result = await sut.GetDirectoryContentsAsync("/");

        Assert.Collection(
            result.Entries,
            entry =>
            {
                Assert.Equal("Documents", entry.Name);
                Assert.Equal(2, entry.ChildCount);
            },
            entry =>
            {
                Assert.Equal("Empty", entry.Name);
                Assert.Equal(0, entry.ChildCount);
            });
    }

    private static AsyncPageable<BlobHierarchyItem> Page(params BlobHierarchyItem[] items)
    {
        var response = Substitute.For<Response>();
        var page = Azure.Page<BlobHierarchyItem>.FromValues(items, null, response);
        return AsyncPageable<BlobHierarchyItem>.FromPages([page]);
    }

    private static BlobHierarchyItem Directory(string prefix) =>
        BlobsModelFactory.BlobHierarchyItem(prefix, null);

    private static BlobHierarchyItem Blob(string name) =>
        BlobsModelFactory.BlobHierarchyItem(
            null,
            BlobsModelFactory.BlobItem(
                name,
                deleted: false,
                properties: null,
                snapshot: null,
                metadata: null));
}
