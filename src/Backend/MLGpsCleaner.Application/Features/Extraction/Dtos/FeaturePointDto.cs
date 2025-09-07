namespace MLGpsCleaner.Application.Features.Extraction.Dtos;

public record FeaturePointDto(
    long Id,
    long DeviceId,
    DateTime DeviceTime,
    double Lat,
    double Lng,
    double SpeedKph,
    double DistanceFromPrevMeters,
    double DeltaTimeSeconds,
    double AccelerationKphPerSec,
    double BearingDeg,
    double BearingChangeDeg,
    double CrossTrackMeters
);