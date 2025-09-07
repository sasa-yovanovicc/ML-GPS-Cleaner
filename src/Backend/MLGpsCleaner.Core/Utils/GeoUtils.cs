namespace MLGpsCleaner.Core.Utils;

public static class GeoUtils
{
    private const double EarthRadiusMeters = 6371000.0; // mean radius

    public static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = ToRad(lat2 - lat1);
        double dLon = ToRad(lon2 - lon1);
        lat1 = ToRad(lat1); lat2 = ToRad(lat2);
        double a = Math.Sin(dLat/2)*Math.Sin(dLat/2) + Math.Cos(lat1)*Math.Cos(lat2)*Math.Sin(dLon/2)*Math.Sin(dLon/2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a));
        return EarthRadiusMeters * c;
    }

    public static double BearingDegrees(double lat1, double lon1, double lat2, double lon2)
    {
        // φ (phi) = latitude in radians, λ (lambda) = longitude in radians
        var φ1 = ToRad(lat1); // phi1
        var φ2 = ToRad(lat2); // phi2
        var λ1 = ToRad(lon1); // lambda1
        var λ2 = ToRad(lon2); // lambda2
        var y = Math.Sin(λ2-λ1) * Math.Cos(φ2); // projection component
        var x = Math.Cos(φ1)*Math.Sin(φ2) - Math.Sin(φ1)*Math.Cos(φ2)*Math.Cos(λ2-λ1);
        var brng = Math.Atan2(y,x); // θ (theta) bearing in radians
        return (ToDeg(brng)+360)%360; // convert to degrees normalized 0..360
    }

    // Cross-track distance of point C from segment AB (in meters)
    public static double CrossTrackDistanceMeters(double latA,double lonA,double latB,double lonB,double latC,double lonC)
    {
        // Convert geographic coordinates to radians.
        // φ (phi) latitude, λ (lambda) longitude; indices 1=A,2=B,3=C.
        var φ1 = ToRad(latA); var λ1 = ToRad(lonA); // phi1 / lambda1 (point A)
        var φ2 = ToRad(latB); var λ2 = ToRad(lonB); // phi2 / lambda2 (point B)
        var φ3 = ToRad(latC); var λ3 = ToRad(lonC); // phi3 / lambda3 (point C)

        // δ13 (delta13): angular distance between A and C on the sphere.
        var δ13 = AngularDistance(φ1, λ1, φ3, λ3);
        // θ13 (theta13): initial bearing from A to C.
        var θ13 = BearingRadians(φ1, λ1, φ3, λ3);
        // θ12 (theta12): initial bearing from A to B.
        var θ12 = BearingRadians(φ1, λ1, φ2, λ2);
        // δxt (delta cross-track): cross-track angular distance.
        var δxt = Math.Asin(Math.Sin(δ13) * Math.Sin(θ13-θ12));
        return Math.Abs(δxt) * EarthRadiusMeters; // Convert angular distance to meters.
    }

    private static double AngularDistance(double φ1,double λ1,double φ2,double λ2)
    {
        // Great-circle angular distance between two points (A = (φ1,λ1), B = (φ2,λ2)).
        var dφ = φ2-φ1; // delta phi
        var dλ = λ2-λ1; // delta lambda
        var a = Math.Sin(dφ/2)*Math.Sin(dφ/2)+Math.Cos(φ1)*Math.Cos(φ2)*Math.Sin(dλ/2)*Math.Sin(dλ/2);
        var c = 2*Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a)); // central angle
        return c; // radians
    }
    private static double BearingRadians(double φ1,double λ1,double φ2,double λ2)
    {
        // Returns θ (theta) initial bearing from point 1 to point 2 in radians.
        var y = Math.Sin(λ2-λ1)*Math.Cos(φ2);
        var x = Math.Cos(φ1)*Math.Sin(φ2)-Math.Sin(φ1)*Math.Cos(φ2)*Math.Cos(λ2-λ1);
        return Math.Atan2(y,x);
    }

    private static double ToRad(double v)=> v * Math.PI/180.0;
    private static double ToDeg(double v)=> v * 180.0/Math.PI;
}
