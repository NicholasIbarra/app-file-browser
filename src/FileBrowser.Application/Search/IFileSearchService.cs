using FileBrowser.Application.Search.Semantic;
using System;
using System.Collections.Generic;
using System.Text;

namespace FileBrowser.Application.Search;

public interface IFileSearchService
{
    Task<SemanticFileSearchResponseDto> SearchSemanticAsync(
        string query, int limit = 50, CancellationToken cancellationToken = default);

    IReadOnlyList<FileSearchResultDto> SearchAsync(
        string query,
        int limit = 50,
        CancellationToken cancellationToken = default);
}
