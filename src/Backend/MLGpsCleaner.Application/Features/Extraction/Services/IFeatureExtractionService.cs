using MLGpsCleaner.Application.Features.Extraction.Dtos;
using MLGpsCleaner.Application.Features.Positions.Dtos;

namespace MLGpsCleaner.Application.Features.Extraction.Services;

public interface IFeatureExtractionService
{
    IReadOnlyList<FeaturePointDto> Extract(IReadOnlyList<GpsPointDto> points);
}