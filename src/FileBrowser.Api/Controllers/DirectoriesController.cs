using FileBrowser.Application.Abtractstions.Indexing;
using FileBrowser.Application.Browsing;
using Microsoft.AspNetCore.Mvc;

namespace FileBrowser.Api.Controllers;

[ApiController]
[Route("api/directories")]
public class DirectoriesController : ControllerBase
{
    private readonly IDirectoryBrowserService _directoryBrowserService;

    public DirectoriesController(IDirectoryBrowserService directoryBrowserService)
    {
        _directoryBrowserService = directoryBrowserService;
    }

    [HttpGet]
    public async Task<ActionResult<DirectoryContentsDto>> GetAsync(
        [FromQuery] string? path,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        var result = await _directoryBrowserService.GetDirectoryContentsAsync(
            path,
            sort,
            cancellationToken);

        return Ok(result);
    }
}
