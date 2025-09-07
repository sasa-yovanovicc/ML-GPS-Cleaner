using MLGpsCleaner.Application.Features.Cleaning.Dtos;
using MLGpsCleaner.Application.Features.Positions.Dtos;
using MLGpsCleaner.Application.Features.Extraction.Services;
using MLGpsCleaner.Application.Features.Extraction.Dtos;

namespace MLGpsCleaner.Application.Features.Cleaning.Services;

public class RouteCleaningService : IRouteCleaningService
{
    private readonly IFeatureExtractionService _features;
    public RouteCleaningService(IFeatureExtractionService features) => _features = features;

    public IReadOnlyList<CleanedPointDto> Clean(IReadOnlyList<GpsPointDto> raw, RouteCleaningOptions? options = null)
    {
        options ??= new RouteCleaningOptions();
        if (raw.Count == 0) return Array.Empty<CleanedPointDto>();
        if (raw.Count == 1) return new [] { new CleanedPointDto(raw[0].Id, raw[0].DeviceId, raw[0].DeviceTime, raw[0].Lat, raw[0].Lng, raw[0].SpeedKph,false,false)};

        var feats = _features.Extract(raw);
        var cleaned = new List<CleanedPointDto>(feats.Count);

        // Teleport detection: mark as outlier if jump > maxJumpMeters
        double maxJumpMeters = options.MaxJumpMeters > 0 ? options.MaxJumpMeters : 500; // default 500m
        var teleportOutlierIdx = new HashSet<int>();
        for (int i = 1; i < raw.Count; i++)
        {
            double dist = Haversine(raw[i-1].Lat, raw[i-1].Lng, raw[i].Lat, raw[i].Lng);
            if (dist > maxJumpMeters)
            {
                teleportOutlierIdx.Add(i-1);
                teleportOutlierIdx.Add(i);
            }
        }

        // Median-of-five filter for position outliers
        int window = 2; // 2 left + 2 right + self = 5
        var medianOutlierIdx = new HashSet<int>();
        if (raw.Count >= 5)
        {
            for (int i = window; i < raw.Count - window; i++)
            {
                var lats = new List<double>();
                var lngs = new List<double>();
                for (int j = i - window; j <= i + window; j++)
                {
                    lats.Add(raw[j].Lat);
                    lngs.Add(raw[j].Lng);
                }
                lats.Sort(); lngs.Sort();
                double medLat = lats[window];
                double medLng = lngs[window];
                double dist = Haversine(raw[i].Lat, raw[i].Lng, medLat, medLng);
                if (dist > 100) // prag za outlier, npr. 100m
                    medianOutlierIdx.Add(i);
            }
        }

        // Optional Hampel filter (on speed) to pre-mark spikes
        bool[] hampelOut = options.UseHampelOnSpeed ? ApplyHampel(feats.Select(f=>f.SpeedKph).ToArray(), options.HampelWindow, options.HampelNSigma) : new bool[feats.Count];
        for (int i=0;i<feats.Count;i++)
        {
            var f = feats[i];
            bool isOutlier = false;
            bool isInterpolated = false;
            if (f.SpeedKph > options.MaxSpeedKph) isOutlier = true;
            if (hampelOut[i]) isOutlier = true;
            if (Math.Abs(f.AccelerationKphPerSec) > options.MaxAccelerationKphPerSec) isOutlier = true;
            if (f.DeltaTimeSeconds > 0 && (f.BearingChangeDeg / Math.Max(f.DeltaTimeSeconds,1)) > options.MaxBearingChangeDegPerSec) isOutlier = true;
            if (f.CrossTrackMeters > options.MaxCrossTrackMeters) isOutlier = true;
            if (teleportOutlierIdx.Contains(i)) isOutlier = true;
            if (medianOutlierIdx.Contains(i)) isOutlier = true;
            cleaned.Add(new CleanedPointDto(f.Id,f.DeviceId,f.DeviceTime,f.Lat,f.Lng,f.SpeedKph,isOutlier,isInterpolated));
        }

        // Block interpolation for consecutive outliers
        int idx = 0;
        while (idx < cleaned.Count)
        {
            if (!cleaned[idx].IsOutlier) { idx++; continue; }
            int start = idx;
            while (idx < cleaned.Count && cleaned[idx].IsOutlier) idx++;
            int end = idx - 1;
            int prevIdx = start - 1;
            int nextIdx = idx;
            if (prevIdx >= 0 && nextIdx < cleaned.Count)
            {
                var a = cleaned[prevIdx]; var b = cleaned[nextIdx];
                var total = (b.DeviceTime - a.DeviceTime).TotalSeconds;
                for (int j = start; j <= end; j++)
                {
                    var rel = total > 0 ? (cleaned[j].DeviceTime - a.DeviceTime).TotalSeconds / total : 0.5;
                    var lat = a.Lat + (b.Lat - a.Lat) * rel;
                    var lng = a.Lng + (b.Lng - a.Lng) * rel;
                    cleaned[j] = cleaned[j] with { Lat = lat, Lng = lng, IsInterpolated = true };
                }
            }
        }
        return cleaned;
    }

    // Haversine formula for distance in meters
    public static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        double R = 6371000; // meters
        double dLat = (lat2 - lat1) * Math.PI / 180.0;
        double dLon = (lon2 - lon1) * Math.PI / 180.0;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                  Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                  Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    public static bool[] ApplyHampel(double[] series, int window, double nSigma)
    {
        if (window < 1) return new bool[series.Length];
        int k = window;
        var flags = new bool[series.Length];
        double scale = 1.4826; // median absolute deviation scale for normal dist
        for (int i=0;i<series.Length;i++)
        {
            int start = Math.Max(0, i-k);
            int end = Math.Min(series.Length-1, i+k);
            int len = end-start+1;
            var windowVals = new double[len];
            Array.Copy(series, start, windowVals, 0, len);
            Array.Sort(windowVals);
            double median = windowVals[len/2];
            // MAD
            var devs = new double[len];
            for (int j=0;j<len;j++) devs[j] = Math.Abs(windowVals[j]-median);
            Array.Sort(devs);
            double mad = devs[len/2];
            if (mad == 0) continue;
            double thresh = nSigma * scale * mad;
            if (Math.Abs(series[i]-median) > thresh) flags[i] = true;
        }
        return flags;
    }
}

