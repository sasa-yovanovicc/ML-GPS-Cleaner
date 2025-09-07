using MLGpsCleaner.Core.Models;

namespace MLGpsCleaner.Core.Abstractions;

public interface ICleaningFeedbackRepository
{
    Task AddAsync(CleaningFeedback fb, CancellationToken ct = default);
    Task<CleaningFeedback?> GetAsync(long deviceId, DateTime day, CancellationToken ct = default);
}
