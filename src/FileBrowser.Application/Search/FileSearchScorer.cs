using FileBrowser.Application.Abtractstions.Indexing;

namespace FileBrowser.Application.Search;

public sealed class FileSearchScorer : IFileSearchScorer
{
    public FileSearchMatch? Score(
        FileIndexEntry entry,
        string query)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        if (entry.Name.Equals(
                query,
                StringComparison.OrdinalIgnoreCase))
        {
            return new(1000, FileSearchMatchType.ExactName);
        }

        if (entry.Name.StartsWith(
                query,
                StringComparison.OrdinalIgnoreCase))
        {
            return new(800, FileSearchMatchType.NamePrefix);
        }

        if (ContainsWordPrefix(entry.Name, query))
        {
            return new(600, FileSearchMatchType.WordPrefix);
        }

        if (entry.Name.Contains(
                query,
                StringComparison.OrdinalIgnoreCase))
        {
            return new(400, FileSearchMatchType.NameContains);
        }

        if (entry.RelativePath.Contains(
                query,
                StringComparison.OrdinalIgnoreCase))
        {
            return new(200, FileSearchMatchType.PathContains);
        }

        return null;
    }

    private static bool ContainsWordPrefix(
        string value,
        string query)
    {
        for (var i = 0; i < value.Length; i++)
        {
            var isWordStart =
                i == 0 ||
                !char.IsLetterOrDigit(value[i - 1]);

            if (!isWordStart)
            {
                continue;
            }

            if (value.AsSpan(i).StartsWith(
                    query,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
