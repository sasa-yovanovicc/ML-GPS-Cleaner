using Microsoft.AspNetCore.Mvc;
using MLGpsCleaner.Application.Features.Devices.Services;

namespace MLGpsCleaner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
    private readonly IDeviceService _service;
    public DevicesController(IDeviceService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var list = await _service.ListAsync();
    return Ok(list.Select(d => new { id = d.Id, name = d.Name, category = d.Category }));
    }
}