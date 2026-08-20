namespace FeatureFinderViewerNet;

interface IFeatureScanService
{
    Task<IReadOnlyList<FeatureNodeItem>> ScanAsync();
}
