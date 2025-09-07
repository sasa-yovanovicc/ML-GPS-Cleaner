namespace MLGpsCleaner.Core.Models;

public class CleaningFeedback
{
    public long Id { get; set; }
    public long DeviceId { get; set; }
    public DateTime Day { get; set; } // UTC date (00:00)
    public bool Accepted { get; set; } // true=OK, false=Rejected
    public string? Comment { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
