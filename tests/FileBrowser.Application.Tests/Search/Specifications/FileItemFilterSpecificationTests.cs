using FileBrowser.Application.Abtractstions.Indexing;
using FileBrowser.Application.Search;
using FileBrowser.Application.Search.Specifications;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileBrowser.Application.Tests.Search.Specifications;

public class FileItemFilterSpecificationTests
{
    [Fact]
    public void Criteria_WhenSearchItemTypeIsNull_ShouldMatchFilesAndDirectories()
    {
        var specification = new FileItemFilterSpecification(null);
        var criteria = specification.Criteria.Compile();

        Assert.True(criteria(CreateEntry(isDirectory: false)));
        Assert.True(criteria(CreateEntry(isDirectory: true)));
    }

    [Fact]
    public void Criteria_WhenSearchItemTypeIsFile_ShouldMatchOnlyFiles()
    {
        var specification =
            new FileItemFilterSpecification(SearchItemType.File);

        var criteria = specification.Criteria.Compile();

        Assert.True(criteria(CreateEntry(isDirectory: false)));
        Assert.False(criteria(CreateEntry(isDirectory: true)));
    }

    [Fact]
    public void Criteria_WhenSearchItemTypeIsDirectory_ShouldMatchOnlyDirectories()
    {
        var specification =
            new FileItemFilterSpecification(SearchItemType.Directory);

        var criteria = specification.Criteria.Compile();

        Assert.True(criteria(CreateEntry(isDirectory: true)));
        Assert.False(criteria(CreateEntry(isDirectory: false)));
    }

    private FileIndexEntry CreateEntry(bool isDirectory)
    {
        return new FileIndexEntry(
            "test",
            "test",
            "test",
            isDirectory,
            "Test",
            null, null,
            default);
    }
}
