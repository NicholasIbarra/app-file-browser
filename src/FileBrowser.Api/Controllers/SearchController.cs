using FileBrowser.Application.Search;
using Microsoft.AspNetCore.Mvc;

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

    [HttpGet]
    public ActionResult<IReadOnlyList<FileSearchResultDto>> Search(
        [FromQuery] string query,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var results = _fileSearchService.SearchAsync(
            query,
            limit,
            cancellationToken);

        return Ok(results);
    }
}
