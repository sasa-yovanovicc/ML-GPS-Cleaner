using Microsoft.EntityFrameworkCore;
using MLGpsCleaner.Core.Abstractions;
using MLGpsCleaner.Core.Models;

namespace MLGpsCleaner.Infrastructure.Repositories;

public class CleaningFeedbackRepository : ICleaningFeedbackRepository
{
    private readonly TraccarDbContext _db;
    public CleaningFeedbackRepository(TraccarDbContext db) => _db = db;

    public async Task AddAsync(CleaningFeedback fb, CancellationToken ct = default)
    {
        // ensure day normalized
        fb.Day = fb.Day.Date;
        _db.CleaningFeedback.Add(fb);
        await _db.SaveChangesAsync(ct);
    }

    public Task<CleaningFeedback?> GetAsync(long deviceId, DateTime day, CancellationToken ct = default)
    {
        var d = day.Date;
        return _db.CleaningFeedback.FirstOrDefaultAsync(f => f.DeviceId == deviceId && f.Day == d, ct);
    }
}
