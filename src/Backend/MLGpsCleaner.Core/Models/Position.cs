using System.ComponentModel.DataAnnotations.Schema;

namespace MLGpsCleaner.Core.Models;

[Table("tc_positions")]
public class Position
{
    [Column("id")] public long Id { get; set; }
    [Column("deviceid")] public long DeviceId { get; set; }
    [Column("devicetime")] public DateTime DeviceTime { get; set; }
    [Column("servertime")] public DateTime ServerTime { get; set; }
    [Column("fixtime")] public DateTime FixTime { get; set; }
    [Column("latitude")] public double Latitude { get; set; }
    [Column("longitude")] public double Longitude { get; set; }
    [Column("altitude")] public double Altitude { get; set; }
    [Column("speed")] public double Speed { get; set; } // knots in traccar
    [Column("course")] public double Course { get; set; }
    [Column("accuracy")] public double? Accuracy { get; set; }
    [Column("attributes")] public string? Attributes { get; set; }
    [Column("valid")] public bool Valid { get; set; }
}
