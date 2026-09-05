using FileBrowser.Application.Abtractstions.Indexing;

namespace FileBrowser.Application.Search.Scorer;

public interface IFileSearchScorer
{
    FileSearchMatch? Score(
        FileIndexEntry entry,
        string query);
}
