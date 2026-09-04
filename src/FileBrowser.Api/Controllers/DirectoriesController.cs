using FileBrowser.Application.Abtractstions.Indexing;
using FileBrowser.Application.Browsing;
using Microsoft.AspNetCore.Mvc;

namespace FileBrowser.Api.Controllers;

[ApiController]
[Route("api/directories")]
public class DirectoriesController : ControllerBase
{
    private readonly IDirectoryBrowserService _directoryBrowserService;
    private readonly IFileSearchIndex _fileSearchIndex;

    public DirectoriesController(
        IDirectoryBrowserService directoryBrowserService,
        IFileSearchIndex fileSearchIndex)
    {
        _directoryBrowserService = directoryBrowserService;
        _fileSearchIndex = fileSearchIndex;
    }

    [HttpGet]
    public async Task<ActionResult<DirectoryContentsDto>> GetAsync(
        [FromQuery] string? path,
        CancellationToken cancellationToken)
    {
        var result = await _directoryBrowserService.GetDirectoryContentsAsync(
            path,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("re-index")]
    public async Task<IActionResult> ReIndexAsync(
        CancellationToken cancellationToken)
    {
        await _fileSearchIndex.RebuildAsync(cancellationToken);

        return NoContent();
    }
}
