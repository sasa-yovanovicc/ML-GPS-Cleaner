using MLGpsCleaner.Core.Abstractions;

namespace MLGpsCleaner.Application.Features.Devices.Services;

public class DeviceService : IDeviceService
{
    private readonly IDeviceRepository _repo;
    public DeviceService(IDeviceRepository repo) => _repo = repo;
    public async Task<IReadOnlyList<(long Id,string Name,string? Category)>> ListAsync(CancellationToken ct = default)
        => (await _repo.ListAsync(ct)).Select(d => (d.Id, d.Name, d.Category)).ToList();
}