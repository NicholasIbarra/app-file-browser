using FileBrowser.Application.Abtractstions.Indexing;
using FileBrowser.Application.Common.Specifications;
using System.Linq.Expressions;

namespace FileBrowser.Application.Search.Specifications;

public sealed class FileItemFilterSpecification
    : ISpecification<FileIndexEntry>
{
    public Expression<Func<FileIndexEntry, bool>> Criteria { get; }

    public FileItemFilterSpecification(SearchItemType? searchItemType)
    {
        Criteria = item =>
            searchItemType == null ||
                (searchItemType == SearchItemType.File && !item.IsDirectory) ||
                (searchItemType == SearchItemType.Directory && item.IsDirectory);
    }
}