using MLGpsCleaner.Application.Features.Extraction.Dtos;
using MLGpsCleaner.Application.Features.Positions.Dtos;
using MLGpsCleaner.Core.Utils;

namespace MLGpsCleaner.Application.Features.Extraction.Services;

public class FeatureExtractionService : IFeatureExtractionService
{
    public IReadOnlyList<FeaturePointDto> Extract(IReadOnlyList<GpsPointDto> points)
    {
        if (points.Count == 0) return Array.Empty<FeaturePointDto>();
        var list = new List<FeaturePointDto>(points.Count);
        double prevBearing = double.NaN;
        for (int i=0;i<points.Count;i++)
        {
            var p = points[i];
            double dist = 0; double dt = 0; double acc = 0; double bearing=double.NaN; double bearingChange=0; double crossTrack=0;
            if (i>0)
            {
                var prev = points[i-1];
                dist = GeoUtils.HaversineMeters(prev.Lat, prev.Lng, p.Lat, p.Lng);
                dt = (p.DeviceTime - prev.DeviceTime).TotalSeconds;
                if (dt>0) acc = (p.SpeedKph - prev.SpeedKph)/dt;
                bearing = GeoUtils.BearingDegrees(prev.Lat, prev.Lng, p.Lat, p.Lng);
                if (!double.IsNaN(prevBearing) && !double.IsNaN(bearing))
                {
                    var rawDiff = Math.Abs(bearing - prevBearing);
                    bearingChange = rawDiff>180 ? 360-rawDiff : rawDiff;
                }
                prevBearing = bearing;
            }
            if (i>0 && i<points.Count-1)
            {
                var prev = points[i-1]; var next = points[i+1];
                crossTrack = GeoUtils.CrossTrackDistanceMeters(prev.Lat, prev.Lng, next.Lat, next.Lng, p.Lat, p.Lng);
            }
            list.Add(new FeaturePointDto(p.Id,p.DeviceId,p.DeviceTime,p.Lat,p.Lng,p.SpeedKph,dist,dt,acc,bearing,bearingChange,crossTrack));
        }
        return list;
    }
}