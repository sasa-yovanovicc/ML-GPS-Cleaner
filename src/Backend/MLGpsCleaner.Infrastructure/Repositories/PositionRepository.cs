using Microsoft.EntityFrameworkCore;
using MLGpsCleaner.Core.Abstractions;
using MLGpsCleaner.Core.Models;

namespace MLGpsCleaner.Infrastructure.Repositories;

public class PositionRepository : IPositionRepository
{
    private readonly TraccarDbContext _db;
    public PositionRepository(TraccarDbContext db) => _db = db;

    public async Task<IReadOnlyList<Position>> GetByDeviceAsync(long deviceId, DateTime? from, DateTime? to, int max = 100000, CancellationToken ct = default)
    {
    var q = _db.Positions.AsNoTracking().Where(p => p.DeviceId == deviceId); // tc_positions has 'valid' column we may want later
        if (from.HasValue) q = q.Where(p => p.DeviceTime >= from);
        if (to.HasValue) q = q.Where(p => p.DeviceTime <= to);
        return await q.OrderBy(p => p.DeviceTime).Take(max).ToListAsync(ct);
    }

    public async Task<(DateTime? Min, DateTime? Max)> GetDeviceTimeRangeAsync(long deviceId, CancellationToken ct = default)
    {
    var q = _db.Positions.AsNoTracking().Where(p => p.DeviceId == deviceId);
        var min = await q.MinAsync(p => (DateTime?)p.DeviceTime, ct);
        var max = await q.MaxAsync(p => (DateTime?)p.DeviceTime, ct);
        return (min, max);
    }

    public async Task<List<(DateTime Date, int Count)>> GetActiveDaysAsync(long deviceId, int year, int? month, bool onlyValid = true, CancellationToken ct = default)
    {
        var start = new DateTime(year,1,1,0,0,0,DateTimeKind.Utc);
        var end = start.AddYears(1);
        var q = _db.Positions.AsNoTracking()
            .Where(p => p.DeviceId == deviceId && p.DeviceTime >= start && p.DeviceTime < end);
        if (onlyValid) q = q.Where(p => p.Valid);
        if (month.HasValue)
        {
            var mStart = new DateTime(year, month.Value, 1, 0,0,0, DateTimeKind.Utc);
            var mEnd = mStart.AddMonths(1);
            q = q.Where(p => p.DeviceTime >= mStart && p.DeviceTime < mEnd);
        }
        // MySQL (Pomelo) p.DeviceTime.Date can sometimes complicate translation; use date_trunc via conversion to date (DATE())
                // EF-friendly: group by components (Year/Month/Day) then construct date in memory
        var grouped = await q
            .GroupBy(p => new { p.DeviceTime.Year, p.DeviceTime.Month, p.DeviceTime.Day })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Day, Count = g.Count() })
            .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day)
            .ToListAsync(ct);

        return grouped.Select(g => 
        {
            // DateTimeKind.Utc to match other time values
            var date = new DateTime(g.Year, g.Month, g.Day, 0, 0, 0, DateTimeKind.Utc);
            return (date, g.Count);
        }).ToList();
    }
}
