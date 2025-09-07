using MLGpsCleaner.Application.Features.Positions.Dtos;
using MLGpsCleaner.Core.Abstractions;

namespace MLGpsCleaner.Application.Features.Positions.Services;

public class PositionService : IPositionService
{
    private readonly IPositionRepository _repo;
    public PositionService(IPositionRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<GpsPointDto>> GetRawAsync(long deviceId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var list = await _repo.GetByDeviceAsync(deviceId, from, to, ct: ct);
        return list.Select(p => new GpsPointDto(p.Id, p.DeviceId, p.DeviceTime, p.Latitude, p.Longitude, p.Speed * 1.852)) // knots->kph
                   .ToList();
    }

    public Task<(DateTime? Min, DateTime? Max)> GetTimeRangeAsync(long deviceId, CancellationToken ct = default)
        => _repo.GetDeviceTimeRangeAsync(deviceId, ct);

    public Task<List<(DateTime Date, int Count)>> GetActiveDaysAsync(long deviceId, int year, int? month, bool onlyValid = true, CancellationToken ct = default)
        => _repo.GetActiveDaysAsync(deviceId, year, month, onlyValid: onlyValid, ct: ct);
}
