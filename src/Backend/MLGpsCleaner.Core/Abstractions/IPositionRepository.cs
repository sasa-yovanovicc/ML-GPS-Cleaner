using MLGpsCleaner.Core.Models;

namespace MLGpsCleaner.Core.Abstractions;

public interface IPositionRepository
{
    Task<IReadOnlyList<Position>> GetByDeviceAsync(long deviceId, DateTime? from, DateTime? to, int max = 100000, CancellationToken ct = default);
    Task<(DateTime? Min, DateTime? Max)> GetDeviceTimeRangeAsync(long deviceId, CancellationToken ct = default);
    Task<List<(DateTime Date, int Count)>> GetActiveDaysAsync(long deviceId, int year, int? month, bool onlyValid = true, CancellationToken ct = default);
}
