using MLGpsCleaner.Core.Models;

namespace MLGpsCleaner.Core.Abstractions;

public interface IDeviceRepository
{
    Task<IReadOnlyList<Device>> ListAsync(CancellationToken ct = default);
}