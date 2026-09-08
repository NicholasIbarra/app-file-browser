using FileBrowser.Application.Search;
using FileBrowser.Application.Search.Semantic;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FileBrowser.Api.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly IFileSearchService _fileSearchService;

    public SearchController(IFileSearchService fileSearchService)
    {
        _fileSearchService = fileSearchService;
    }

    [HttpGet("semantic")]
    public async Task<ActionResult<SemanticFileSearchResponseDto>> SearchSemantic(
        [FromQuery, Required] string query,
        [FromQuery, Range(1, int.MaxValue)] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var results = await _fileSearchService.SearchSemanticAsync(query, limit, cancellationToken);
        return Ok(results);
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<FileSearchResultDto>> Search(
        [FromQuery] string query,
        [FromQuery] int limit = 50,
        [FromQuery] SearchItemType? searchItemType = null,
        CancellationToken cancellationToken = default)
    {
        var results = _fileSearchService.SearchAsync(
            query,
            limit,
            searchItemType,
            cancellationToken);

        return Ok(results);
    }
}
