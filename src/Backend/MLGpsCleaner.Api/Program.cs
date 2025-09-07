using MLGpsCleaner.Infrastructure;
using MLGpsCleaner.Application;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TraccarDbContext>(opt =>
    opt.UseMySql(builder.Configuration.GetConnectionString("Traccar"), ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("Traccar"))));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DI registrations (Infrastructure / Application)
builder.Services.AddInfrastructure();
builder.Services.AddApplication();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// Lightweight health endpoint for dev/proxy auto-detection
app.MapGet("/api/health", () => Results.Json(new { status = "ok", timeUtc = DateTime.UtcNow }));

app.Run();
