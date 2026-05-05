using CAMAPI.FeatureFinder;

namespace FeatureFinderViewerNet;

interface IFeatureFinderView
{
    void SetBusy(bool busy);
    void ShowError(string message);
    void ShowFeatureTypes(IEnumerable<(TCamApiFeatureType Type, int Count)> groups);
    void ShowFeatureItems(IEnumerable<FeatureNodeItem> items);
    void ShowEntityPaths(IEnumerable<string> entityPaths);
    void ShowProperties(IReadOnlyList<PropertyItem> properties);
    void SelectAllEntities();
}
