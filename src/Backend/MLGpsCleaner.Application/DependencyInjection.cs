using Microsoft.Extensions.DependencyInjection;
using MLGpsCleaner.Application.Features.Positions.Services;
using MLGpsCleaner.Application.Features.Cleaning.Services;
using MLGpsCleaner.Application.Features.Extraction.Services;
using MLGpsCleaner.Application.Features.Devices.Services;

namespace MLGpsCleaner.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
    services.AddScoped<IPositionService, PositionService>();
    services.AddScoped<IRouteCleaningService, RouteCleaningService>();
    services.AddScoped<IFeatureExtractionService, FeatureExtractionService>();
    services.AddScoped<IDeviceService, DeviceService>();
        return services;
    }
}
