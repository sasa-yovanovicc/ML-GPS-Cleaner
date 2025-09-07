using Microsoft.EntityFrameworkCore;
using MLGpsCleaner.Core.Models;

namespace MLGpsCleaner.Infrastructure;

public class TraccarDbContext : DbContext
{
    public TraccarDbContext(DbContextOptions<TraccarDbContext> options) : base(options) {}

    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<CleaningFeedback> CleaningFeedback => Set<CleaningFeedback>();
}
