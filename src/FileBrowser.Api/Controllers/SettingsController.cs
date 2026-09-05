using FileBrowser.Api.Models;
using FileBrowser.Infrastructure.AI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FileBrowser.Api.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly AzureOpenAiOptions _options;

    public SettingsController(IOptions<AzureOpenAiOptions> options)
    {
        _options = options.Value;
    }

    [HttpGet]
    public ActionResult<SettingsResponse> Get()
    {
        return Ok(new SettingsResponse(_options.Enabled));
    }
}
