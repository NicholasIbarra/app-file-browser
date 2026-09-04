using FileBrowser.Application.Abtractstions.Indexing;
using FileBrowser.Application.Browsing;
using FileBrowser.Api.Models;
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

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAsync(
        [FromQuery] string path,
        [FromQuery] bool overwrite,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        await _directoryBrowserService.UploadAsync(
            path,
            content,
            overwrite,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAsync(
        [FromQuery] string path,
        [FromQuery] bool recursive,
        CancellationToken cancellationToken)
    {
        await _directoryBrowserService.DeleteAsync(path, recursive, cancellationToken);
        return NoContent();
    }

    [HttpPost("move")]
    public async Task<IActionResult> MoveAsync(
        [FromBody] FileOperationRequest request,
        CancellationToken cancellationToken)
    {
        await _directoryBrowserService.MoveAsync(
            request.SourcePath,
            request.DestinationPath,
            request.Overwrite,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("copy")]
    public async Task<IActionResult> CopyAsync(
        [FromBody] FileOperationRequest request,
        CancellationToken cancellationToken)
    {
        await _directoryBrowserService.CopyAsync(
            request.SourcePath,
            request.DestinationPath,
            request.Overwrite,
            cancellationToken);

        return NoContent();
    }
}
