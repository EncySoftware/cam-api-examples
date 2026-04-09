using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using CAMAPI.DotnetHelper;
using CAMAPI.ModelFormerTypes;

namespace SetFeedZone;

/// <summary>
/// Interaction logic for ViewControlWindow.xaml
/// </summary>
public partial class ViewControlWindow : Window, IDisposable
{
    private readonly ComWrapper<ICamApiModelFormer> _modelFormerCom;

    /// <summary>
    /// List of curves to display
    /// </summary>
    public ObservableCollection<CurveItem> Curves { get; } = [];

    /// <summary>
    /// Interaction logic for ViewControlWindow.xaml
    /// </summary>
    public ViewControlWindow(ComWrapper<ICamApiModelFormer> modelFormerCom)
    {
        _modelFormerCom = modelFormerCom;
        InitializeComponent();
        DataGridFeedType.ItemsSource = Enum.GetValues(typeof(TModelFormerFeedType));
        DataGridFeedChangeMode.ItemsSource = Enum.GetValues(typeof(TModelFormerFeedRateChangeType));
        ReloadCurves();
    }

    /// <summary>
    /// Dispose all curves and clean the list
    /// </summary>
    private void CleanCurves()
    {
        foreach (var curve in Curves)
            curve.Dispose();
        Curves.Clear();
    }
    
    /// <summary>
    /// Clean the list of curves and reload them from the API
    /// </summary>
    private void ReloadCurves()
    {
        var selectedName = (CurvesListBox.SelectedItem as CurveItem)?.Name;

        // calculate items
        CleanCurves();
        CurveItem? itemToSelect = null;
        foreach (var modelItemCom in _modelFormerCom.Items())
        {
            using var modelItem5DCurveCom = modelItemCom.AsInstanceOf<ICamApiCurve5DModelItem>();
            if (modelItem5DCurveCom == null)
                continue;

            // add
            var curveName = modelItemCom.Caption();
            var curve = new CurveItem(modelItem5DCurveCom)
            {
                Name = curveName
            };
            Curves.Add(curve);
            
            // select
            if (itemToSelect == null || curve.Name == selectedName)
               itemToSelect = curve;
        }

        // refresh UI
        CurvesListBox.ItemsSource = null;
        CurvesListBox.ItemsSource = Curves;
        CurvesListBox.SelectedItem = itemToSelect;
    }
    
    /// <summary>
    /// Return the selected curve. May return null if nothing is selected
    /// </summary>
    /// <returns></returns>
    private CurveItem? GetSelectedCurve()
    {
        if (CurvesListBox.SelectedItem is CurveItem curve)
            return curve;
        return null;
    }

    private void CurvesListBox_SelectionChanged(object sender, SelectionChangedEventArgs? e)
    {
        try
        {
            var selectedCurve = GetSelectedCurve();
            if (selectedCurve == null)
            {
                FeedZonesDataGrid.ItemsSource = null;
                return;
            }
            selectedCurve.ReloadFeedZones(FeedZonesDataGrid);
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.ToString());
        }
    }

    /// <summary>
    /// Refresh curves button click handler
    /// </summary>
    private void RefreshCurves_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ReloadCurves();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.ToString());
        }
    }

    private void AddFeedZone_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedCurve = GetSelectedCurve();
            if (selectedCurve == null)
                throw new Exception("No curve selected");
            selectedCurve.AddFeedZone();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.ToString());
        }
    }

    private void DeleteFeedZone_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var selectedCurve = GetSelectedCurve();
            if (selectedCurve == null)
                throw new Exception("No curve selected");
            selectedCurve.DeleteSelectedFeedZone(FeedZonesDataGrid);
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.ToString());
        }
    }

    private void FeedZonesDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        try
        {
            var selectedCurve = GetSelectedCurve();
            if (selectedCurve == null)
                throw new Exception("No curve selected");
            
            if (e.EditAction != DataGridEditAction.Commit)
                return;
            if (e.Row.Item is not FeedZoneItem editedItem)
                return;

            using var itemsCom = selectedCurve.CurveModelItemCom.FeedPoints();
            var index = editedItem.Index;

            switch (e.Column)
            {
                case DataGridTextColumn textColumn:
                {
                    var bindingPath = (textColumn.Binding as System.Windows.Data.Binding)?.Path.Path;
                    switch (bindingPath)
                    {
                        case "Length" when e.EditingElement is TextBox lengthBox && double.TryParse(lengthBox.Text, out var length):
                            itemsCom.SetLength(index, length);
                            break;
                        case "Percentage" when e.EditingElement is TextBox percentageBox && int.TryParse(percentageBox.Text, out var percentage):
                            itemsCom.SetFeedRatePercentage(index, percentage);
                            break;
                    }

                    break;
                }
                case DataGridComboBoxColumn comboColumn:
                {
                    var bindingPath = (comboColumn.SelectedItemBinding as System.Windows.Data.Binding)?.Path.Path;
                    switch (bindingPath)
                    {
                        case "FeedType" when e.EditingElement is ComboBox { SelectedItem: TModelFormerFeedType feedType }:
                            itemsCom.SetFeedType(index, feedType);
                            // Refresh IsRapid in item (it will be updated via property setter, but let's be sure UI updates)
                            break;
                        case "ChangeMode" when e.EditingElement is ComboBox { SelectedItem: TModelFormerFeedRateChangeType changeMode }:
                            itemsCom.SetFeedRateChangeType(index, changeMode);
                            break;
                    }

                    break;
                }
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.ToString());
        }
    }



    /// <inheritdoc />
    public void Dispose()
    {
        _modelFormerCom.Dispose();
        foreach (var curve in Curves)
            curve.Dispose();
    }
}