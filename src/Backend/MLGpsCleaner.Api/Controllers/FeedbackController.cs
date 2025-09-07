using Microsoft.AspNetCore.Mvc;
using MLGpsCleaner.Core.Abstractions;
using MLGpsCleaner.Core.Models;

namespace MLGpsCleaner.Api.Controllers;

[ApiController]
[Route("api/feedback")]
public class FeedbackController : ControllerBase
{
    private readonly ICleaningFeedbackRepository _repo;
    public FeedbackController(ICleaningFeedbackRepository repo) => _repo = repo;

    public record SubmitFeedbackRequest(long DeviceId, DateTime Day, bool Accepted, string? Comment);

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitFeedbackRequest req, CancellationToken ct)
    {
        var existing = await _repo.GetAsync(req.DeviceId, req.Day, ct);
        if (existing != null) return Conflict(new { message = "Feedback already exists" });
        await _repo.AddAsync(new CleaningFeedback { DeviceId = req.DeviceId, Day = req.Day.Date, Accepted = req.Accepted, Comment = req.Comment }, ct);
        return Ok(new { status = "saved" });
    }

    [HttpGet]
    public async Task<IActionResult> Get(long deviceId, DateTime day, CancellationToken ct)
    {
        var fb = await _repo.GetAsync(deviceId, day, ct);
        return fb == null ? NotFound() : Ok(fb);
    }
}
