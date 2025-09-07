namespace MLGpsCleaner.Application.Features.Cleaning.Dtos;

public record CleanedPointDto(long Id,long DeviceId,DateTime DeviceTime,double Lat,double Lng,double SpeedKph,bool IsOutlier,bool IsInterpolated);
