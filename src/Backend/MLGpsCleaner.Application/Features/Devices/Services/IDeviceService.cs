using MLGpsCleaner.Core.Models;

namespace MLGpsCleaner.Application.Features.Devices.Services;

public interface IDeviceService
{
    Task<IReadOnlyList<(long Id,string Name,string? Category)>> ListAsync(CancellationToken ct = default);
}