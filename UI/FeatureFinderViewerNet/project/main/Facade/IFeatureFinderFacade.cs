namespace FeatureFinderViewerNet;

interface IFeatureFinderFacade
{
    Task<IReadOnlyList<FeatureNodeItem>> ScanFeaturesAsync();
    void SelectEntities(IEnumerable<string> entityPaths);
}
