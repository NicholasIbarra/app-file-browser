using FileBrowser.Application.Abtractstions.Indexing;

namespace FileBrowser.Application.Search;

public interface IFileSearchScorer
{
    FileSearchMatch? Score(
        FileIndexEntry entry,
        string query);
}
