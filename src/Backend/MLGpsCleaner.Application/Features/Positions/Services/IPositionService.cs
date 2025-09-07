using MLGpsCleaner.Application.Features.Positions.Dtos;

namespace MLGpsCleaner.Application.Features.Positions.Services;

public interface IPositionService
{
    Task<IReadOnlyList<GpsPointDto>> GetRawAsync(long deviceId, DateTime? from, DateTime? to, CancellationToken ct = default);
    Task<(DateTime? Min, DateTime? Max)> GetTimeRangeAsync(long deviceId, CancellationToken ct = default);
    Task<List<(DateTime Date, int Count)>> GetActiveDaysAsync(long deviceId, int year, int? month, bool onlyValid = true, CancellationToken ct = default);
}
