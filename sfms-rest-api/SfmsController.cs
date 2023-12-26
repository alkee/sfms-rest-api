using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using sfms;

namespace sfms_rest_api;

[ApiController]
public class SfmsController
    : ControllerBase
{
    private readonly ILogger<SfmsController> logger;
    private readonly Container container;

    public SfmsController(ILogger<SfmsController> logger, Container container)
    {
        this.logger = logger;
        this.container = container;
    }

    [HttpGet("test")]
    public async Task<IActionResult> Test()
    {
        await Task.Yield();

        return Ok();
    }
}
