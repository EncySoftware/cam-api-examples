using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.FeatureFinder;
using FeatureFinderViewerNet.Controller;
using FeatureFinderViewerNet.Facade;
using FeatureFinderViewerNet.Services;

namespace FeatureFinderViewerNet;

public partial class FeatureFinderViewerWindow : Window, IDisposable, IFeatureFinderView
{
    private readonly ComWrapper<ICamApiApplication> _appCom;
    private readonly FeatureFinderController _controller;

    public FeatureFinderViewerWindow(ComWrapper<ICamApiApplication> appCom)
    {
        _appCom = appCom;
        InitializeComponent();

        var scanService = new FeatureScanService(appCom);
        var selectionService = new ViewportSelectionService(appCom);
        var facade = new FeatureFinderFacade(scanService, selectionService);
        _controller = new FeatureFinderController(facade, this);

        Loaded += (_, _) => _ = _controller.StartScanAsync();
    }

    /// <inheritdoc />
    public void Dispose() => _appCom.Dispose();

    // ── IFeatureFinderView ────────────────────────────────────────────────────

    public void SetBusy(bool busy)
    {
        BtnRefresh.IsEnabled = !busy;
        BusyIndicator.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
    }

    public void ShowError(string message)
    {
        StatusText.Foreground = Brushes.Red;
        StatusText.Text = $"Error: {message}";
    }

    public void ShowFeatureTypes(IEnumerable<(TCamApiFeatureType Type, int Count)> groups)
    {
        StatusText.Text = "";
        StatusText.Foreground = Brushes.Black;
        LeftList.Items.Clear();
        RightList.Items.Clear();
        EntityList.Items.Clear();
        InfoList.Items.Clear();
        foreach (var (type, count) in groups)
            LeftList.Items.Add(new FeatureTypeGroup(type, count));
    }

    public void ShowFeatureItems(IEnumerable<FeatureNodeItem> items)
    {
        RightList.Items.Clear();
        EntityList.Items.Clear();
        InfoList.Items.Clear();
        foreach (var item in items)
        {
            var text = string.IsNullOrEmpty(item.KeyInfo)
                ? item.Caption
                : $"{item.Caption}  {item.KeyInfo}";
            RightList.Items.Add(new FeatureListItem(item, text));
        }
    }

    public void ShowEntityPaths(IEnumerable<string> entityPaths)
    {
        EntityList.SelectionChanged -= EntityList_SelectionChanged;
        EntityList.Items.Clear();
        InfoList.Items.Clear();
        foreach (var path in entityPaths)
        {
            var slash = path.LastIndexOf('/');
            var displayName = slash >= 0 ? path[(slash + 1)..] : path;
            EntityList.Items.Add(new EntityItem(path, displayName));
        }
        EntityList.SelectionChanged += EntityList_SelectionChanged;
    }

    public void ShowProperties(IReadOnlyList<PropertyItem> properties)
    {
        InfoList.Items.Clear();
        foreach (var prop in properties)
            InfoList.Items.Add(prop);
    }

    public void SelectAllEntities()
    {
        EntityList.SelectionChanged -= EntityList_SelectionChanged;
        EntityList.SelectAll();
        EntityList.SelectionChanged += EntityList_SelectionChanged;
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void LeftList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var type = (LeftList.SelectedItem as FeatureTypeGroup)?.FeatureType;
        _controller.OnFeatureTypeSelected(type);
    }

    private void RightList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var item = (RightList.SelectedItem as FeatureListItem)?.Source;
        _controller.OnFeatureItemSelected(item);
    }

    private void EntityList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var paths = EntityList.SelectedItems
            .Cast<EntityItem>()
            .Select(i => i.EntityPath)
            .ToList();
        _controller.OnEntitiesSelected(paths);
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await _controller.StartScanAsync();
    }

    private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
    {
        EntityList.SelectAll();
    }

    // ── Local view models ─────────────────────────────────────────────────────

    private record FeatureTypeGroup(TCamApiFeatureType FeatureType, int Count)
    {
        public override string ToString()
        {
            var name = FeatureType.ToString().Replace("caft", "");
            return $"{name} ({Count})";
        }
    }

    private record FeatureListItem(FeatureNodeItem Source, string DisplayText)
    {
        public override string ToString() => DisplayText;
    }

    private record EntityItem(string EntityPath, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }
}
