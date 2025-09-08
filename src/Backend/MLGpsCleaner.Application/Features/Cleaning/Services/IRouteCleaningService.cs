using MLGpsCleaner.Application.Features.Cleaning.Dtos;
using MLGpsCleaner.Application.Features.Positions.Dtos;

namespace MLGpsCleaner.Application.Features.Cleaning.Services;

public interface IRouteCleaningService
{
    IReadOnlyList<CleanedPointDto> Clean(IReadOnlyList<GpsPointDto> raw, RouteCleaningOptions? options = null);
}

public record RouteCleaningOptions(
    double MaxSpeedKph = 120,
    double MaxAccelerationKphPerSec = 10,
    double MaxBearingChangeDegPerSec = 40,
    double MaxCrossTrackMeters = 80,
    int HampelWindow = 5,
    double HampelNSigma = 3.0,
    bool UseHampelOnSpeed = true,
    double MaxJumpMeters = 300
);
