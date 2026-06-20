using System.Windows;
using System.Windows.Media;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using SelectedFeaturesViewerNet.Service;

namespace SelectedFeaturesViewerNet;

/// <summary>
/// Non-modal window that lists the features recognized on the geometry nodes currently
/// selected in the viewport. The button click marshals the COM chain
/// (GetActiveProject → FeatureFinder → GetFeaturesForSelected) onto an MTA thread via
/// <see cref="MtaTaskScheduler"/>, because the host is MTA while this window runs on STA.
/// </summary>
public partial class SelectedFeaturesWindow : Window, IDisposable
{
    private readonly ComWrapper<ICamApiApplication> _appCom;

    public SelectedFeaturesWindow(ComWrapper<ICamApiApplication> appCom)
    {
        _appCom = appCom;
        InitializeComponent();

        // Apply the host ENCY light/dark theme once the dispatcher is up.
        Loaded += (_, _) => ThemeService.Apply(this, _appCom);
    }

    /// <inheritdoc />
    public void Dispose() => _appCom.Dispose();

    private async void BtnGetFeatures_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true);
        try
        {
            // COM lives in the host's MTA apartment — run the whole chain there and
            // bring back only plain data for the UI thread to display.
            var rows = await MtaTaskScheduler.Run(ScanSelectedFeatures);

            FeaturesList.Items.Clear();
            foreach (var row in rows)
                FeaturesList.Items.Add(row);

            StatusText.Text = rows.Count == 0
                ? "No features recognized on the selected nodes (select some geometry first)."
                : $"{rows.Count} feature(s) recognized on the selected nodes.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// Runs on an MTA worker thread. Walks the CAMAPI chain and projects each feature to a
    /// plain row; every ComWrapper is disposed before returning.
    /// </summary>
    private List<FeatureRow> ScanSelectedFeatures()
    {
        using var projectCom = _appCom.GetActiveProject();
        using var featureFinderCom = projectCom.FeatureFinder();
        using var featureListCom = featureFinderCom.GetFeaturesForSelected();

        var rows = new List<FeatureRow>();
        foreach (var featureCom in featureListCom.Enumerate())
        {
            rows.Add(new FeatureRow(
                featureCom.Caption(),
                featureCom.FeatureType().ToString(),
                featureCom.Id()));
        }
        return rows;
    }

    private void SetBusy(bool busy)
    {
        BtnGetFeatures.IsEnabled = !busy;
        BusyIndicator.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Plain row shown in the list (no COM references).</summary>
    private sealed record FeatureRow(string Caption, string FeatureType, string Id);
}
