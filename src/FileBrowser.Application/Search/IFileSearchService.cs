using System;
using System.Collections.Generic;
using System.Text;

namespace FileBrowser.Application.Search;

public interface IFileSearchService
{
    IReadOnlyList<FileSearchResultDto> SearchAsync(
        string query,
        int limit = 50,
        CancellationToken cancellationToken = default);
}
