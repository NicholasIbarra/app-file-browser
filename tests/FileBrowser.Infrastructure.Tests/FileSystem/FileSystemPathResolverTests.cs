using FileBrowser.Infrastructure.FileSystem;
using Microsoft.Extensions.Options;

namespace FileBrowser.Infrastructure.Tests.FileSystem;

public sealed class FileSystemPathResolverTests : IDisposable
{
    private readonly string _root;
    private readonly FileSystemPathResolver _sut;

    public FileSystemPathResolverTests()
    {
        _root = Path.Combine(
            Path.GetTempPath(),
            $"file-browser-tests-{Guid.NewGuid():N}");

        Directory.CreateDirectory(_root);

        var options = Options.Create(new FileBrowserOptions
        {
            HomeDirectory = _root
        });

        _sut = new FileSystemPathResolver(options);
    }

    [Fact]
    public void Constructor_EmptyHomeDirectory_ThrowsInvalidOperationException()
    {
        var options = Options.Create(new FileBrowserOptions
        {
            HomeDirectory = string.Empty
        });

        Assert.Throws<InvalidOperationException>(
            () => new FileSystemPathResolver(options));
    }

    [Fact]
    public void Resolve_Root_ReturnsConfiguredHomeDirectory()
    {
        var result = _sut.Resolve("/");

        Assert.Equal(
            Path.GetFullPath(_root),
            result);
    }

    [Fact]
    public void Resolve_EmptyPath_ReturnsConfiguredHomeDirectory()
    {
        var result = _sut.Resolve(string.Empty);

        Assert.Equal(
            Path.GetFullPath(_root),
            result);
    }

    [Fact]
    public void Resolve_RelativePath_ReturnsPhysicalPath()
    {
        var result = _sut.Resolve("/Documents/Reports");

        var expected = Path.GetFullPath(
            Path.Combine(_root, "Documents", "Reports"));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Resolve_PathWithoutLeadingSlash_ReturnsPhysicalPath()
    {
        var result = _sut.Resolve("Documents/Reports");

        var expected = Path.GetFullPath(
            Path.Combine(_root, "Documents", "Reports"));

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("../")]
    [InlineData("../../")]
    [InlineData("../../etc")]
    [InlineData("../secrets.txt")]
    [InlineData("Documents/../../../Windows")]
    [InlineData("/Documents/../../../outside")]
    public void Resolve_PathTraversal_ThrowsUnauthorizedAccessException(
        string path)
    {
        Assert.Throws<UnauthorizedAccessException>(
            () => _sut.Resolve(path));
    }

    [Fact]
    public void ToRelativePath_Root_ReturnsSlash()
    {
        var result = _sut.ToRelativePath(_root);

        Assert.Equal("/", result);
    }

    [Fact]
    public void ToRelativePath_NestedPath_ReturnsUrlStylePath()
    {
        var physicalPath = Path.Combine(
            _root,
            "Documents",
            "Reports",
            "report.pdf");

        var result = _sut.ToRelativePath(physicalPath);

        Assert.Equal(
            "/Documents/Reports/report.pdf",
            result);
    }

    [Fact]
    public void ToRelativePath_PathOutsideRoot_ThrowsUnauthorizedAccessException()
    {
        var outsidePath = Path.Combine(
            Path.GetTempPath(),
            $"outside-{Guid.NewGuid():N}",
            "secret.txt");

        Assert.Throws<UnauthorizedAccessException>(
            () => _sut.ToRelativePath(outsidePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}