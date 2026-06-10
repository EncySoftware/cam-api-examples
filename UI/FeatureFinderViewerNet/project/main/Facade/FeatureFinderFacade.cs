namespace FeatureFinderViewerNet.Facade;

class FeatureFinderFacade : IFeatureFinderFacade
{
    private readonly IFeatureScanService _scanService;
    private readonly IViewportSelectionService _selectionService;

    public FeatureFinderFacade(IFeatureScanService scanService, IViewportSelectionService selectionService)
    {
        _scanService = scanService;
        _selectionService = selectionService;
    }

    public Task<IReadOnlyList<FeatureNodeItem>> ScanFeaturesAsync()
        => _scanService.ScanAsync();

    public void SelectEntities(IEnumerable<string> entityPaths)
        => _selectionService.Select(entityPaths);
}
