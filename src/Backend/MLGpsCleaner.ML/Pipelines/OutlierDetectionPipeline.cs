using Microsoft.ML;

namespace MLGpsCleaner.ML.Pipelines;

public static class OutlierDetectionPipeline
{
    // Placeholder for future IsolationForest-like custom implementation or PCA baseline.
    public static ITransformer TrainBaselinePca(MLContext ml, IDataView data)
    {
        // For now return trivial pipeline pass-through until features prepared.
        var pipeline = ml.Transforms.Concatenate("Features") // will adjust later
            ;
        return pipeline.Fit(data);
    }
}
