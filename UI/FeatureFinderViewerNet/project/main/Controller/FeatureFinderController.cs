using CAMAPI.FeatureFinder;

namespace FeatureFinderViewerNet.Controller;

class FeatureFinderController
{
    private readonly IFeatureFinderFacade _facade;
    private readonly IFeatureFinderView _view;
    private Dictionary<TCamApiFeatureType, List<FeatureNodeItem>> _data = new();
    private bool _isScanning;

    public FeatureFinderController(IFeatureFinderFacade facade, IFeatureFinderView view)
    {
        _facade = facade;
        _view = view;
    }

    public async Task StartScanAsync()
    {
        _isScanning = true;
        _view.SetBusy(true);
        try
        {
            var items = await _facade.ScanFeaturesAsync();
            _data = items
                .GroupBy(i => i.FeatureType)
                .ToDictionary(g => g.Key, g => g.ToList());
            _view.ShowFeatureTypes(
                _data.OrderBy(kv => kv.Key.ToString())
                     .Select(kv => (kv.Key, kv.Value.Count)));
        }
        catch (Exception ex)
        {
            _view.ShowError(ex.Message);
        }
        finally
        {
            _isScanning = false;
            _view.SetBusy(false);
        }
    }

    public void OnFeatureTypeSelected(TCamApiFeatureType? type)
    {
        if (type is null || !_data.TryGetValue(type.Value, out var items))
        {
            _view.ShowFeatureItems([]);
            return;
        }
        _view.ShowFeatureItems(items);
    }

    public void OnFeatureItemSelected(FeatureNodeItem? item)
    {
        if (item is null)
        {
            _view.ShowEntityPaths([]);
            _view.ShowProperties([]);
            return;
        }
        _view.ShowEntityPaths(item.EntityPaths);
        _view.ShowProperties(item.Properties);
        _view.SelectAllEntities();
        _facade.SelectEntities(item.EntityPaths);
    }

    public void OnEntitiesSelected(IEnumerable<string> entityPaths)
    {
        if (_isScanning)
            return;
        _facade.SelectEntities(entityPaths);
    }
}
