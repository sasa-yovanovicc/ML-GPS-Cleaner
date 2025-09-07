namespace MLGpsCleaner.Application.Features.Positions.Dtos;

public record GpsPointDto(long Id, long DeviceId, DateTime DeviceTime, double Lat, double Lng, double SpeedKph);
