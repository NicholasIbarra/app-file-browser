using FileBrowser.Api.Models;
using FileBrowser.Application.Files;
using Microsoft.AspNetCore.Mvc;

namespace FileBrowser.Api.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;

    public FilesController(IFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpGet("download")]
    [Produces("application/octet-stream")]
    public async Task<IActionResult> DownloadAsync(
        [FromQuery] string path,
        CancellationToken cancellationToken)
    {
        var download = await _fileService.DownloadAsync(
            path,
            cancellationToken);

        return File(
            download.Content,
            "application/octet-stream",
            download.FileName,
            enableRangeProcessing: true);
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
        var jobId = await _fileService.UploadAsync(
            path,
            content,
            overwrite,
            cancellationToken);

        return Accepted(new { jobId });
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAsync(
        [FromQuery] string path,
        [FromQuery] bool recursive,
        CancellationToken cancellationToken)
    {
        await _fileService.DeleteAsync(path, recursive, cancellationToken);
        return NoContent();
    }

    [HttpPost("move")]
    public async Task<IActionResult> MoveAsync(
        [FromBody] FileOperationRequest request,
        CancellationToken cancellationToken)
    {
        await _fileService.MoveAsync(
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
        await _fileService.CopyAsync(
            request.SourcePath,
            request.DestinationPath,
            request.Overwrite,
            cancellationToken);

        return NoContent();
    }
}
