using Microsoft.AspNetCore.Mvc;
using MLGpsCleaner.Application.Features.Positions.Services;
using MLGpsCleaner.Application.Features.Cleaning.Services;

namespace MLGpsCleaner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PositionsController : ControllerBase
{
    private readonly IPositionService _service;
    private readonly IRouteCleaningService _cleaning;
    public PositionsController(IPositionService service, IRouteCleaningService cleaning)
    { _service = service; _cleaning = cleaning; }

    [HttpGet("device/{deviceId:long}")]
    public async Task<IActionResult> GetByDevice(long deviceId, DateTime? from = null, DateTime? to = null)
    {
    var list = await _service.GetRawAsync(deviceId, from, to);
    return Ok(list);
    }

    [HttpGet("device/{deviceId:long}/cleaned")]
    public async Task<IActionResult> GetCleaned(long deviceId, DateTime? from = null, DateTime? to = null)
    {
        var raw = await _service.GetRawAsync(deviceId, from, to);
        var cleaned = _cleaning.Clean(raw);
        return Ok(new { count = cleaned.Count, rawCount = raw.Count, points = cleaned });
    }

    [HttpGet("device/{deviceId:long}/compare")]
    public async Task<IActionResult> Compare(long deviceId, DateTime? from = null, DateTime? to = null)
    {
        var raw = await _service.GetRawAsync(deviceId, from, to);
        var cleaned = _cleaning.Clean(raw);
        return Ok(new { raw, cleaned });
    }

    [HttpGet("device/{deviceId:long}/range")]
    public async Task<IActionResult> GetRange(long deviceId)
    {
        var (min,max) = await _service.GetTimeRangeAsync(deviceId);
        return Ok(new { min, max });
    }

    // Returns list of day numbers in given month(s) that have activity; if month omitted returns all days of year
    [HttpGet("device/{deviceId:long}/activedays")]
    public async Task<IActionResult> GetActiveDays(long deviceId, int year, int? month = null, bool onlyValid = true)
    {
        // DB aggregation; valid or non-valid positions - onlyValid=false
        var list = await _service.GetActiveDaysAsync(deviceId, year, month, onlyValid);
    var shaped = list.Select(t => new { date = t.Date, count = t.Count });
    return Ok(shaped);
    }
}
