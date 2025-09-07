using Microsoft.Extensions.DependencyInjection;
using MLGpsCleaner.Core.Abstractions;
using MLGpsCleaner.Infrastructure.Repositories;

namespace MLGpsCleaner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
    services.AddScoped<IPositionRepository, PositionRepository>();
    services.AddScoped<IDeviceRepository, DeviceRepository>();
    services.AddScoped<ICleaningFeedbackRepository, CleaningFeedbackRepository>();
        return services;
    }
}
