using System.ComponentModel.DataAnnotations.Schema;

namespace MLGpsCleaner.Core.Models;

[Table("tc_devices")]
public class Device
{
    [Column("id")] public long Id { get; set; }
    [Column("name")] public string Name { get; set; } = string.Empty;
    [Column("category")] public string? Category { get; set; }
}