using Microsoft.EntityFrameworkCore;
using MLGpsCleaner.Core.Abstractions;
using MLGpsCleaner.Core.Models;

namespace MLGpsCleaner.Infrastructure.Repositories;

public class DeviceRepository : IDeviceRepository
{
    private readonly TraccarDbContext _db;
    public DeviceRepository(TraccarDbContext db) => _db = db;
    public async Task<IReadOnlyList<Device>> ListAsync(CancellationToken ct = default)
        => await _db.Devices.AsNoTracking().OrderBy(d=>d.Name).Take(5000).ToListAsync(ct);
}