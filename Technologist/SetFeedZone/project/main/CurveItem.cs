using System.Collections.ObjectModel;
using System.Windows.Controls;
using CAMAPI.DotnetHelper;
using CAMAPI.ModelFormerTypes;

namespace SetFeedZone;

/// <summary>
/// Curve from the job assignment tab
/// </summary>
public class CurveItem : IDisposable
{
    /// <summary>
    /// Curve COM object
    /// </summary>
    public readonly ComWrapper<ICamApiCurve5DModelItem> CurveModelItemCom;

    /// <summary>
    /// Curve from the job assignment tab
    /// </summary>
    public CurveItem(ComWrapper<ICamApiCurve5DModelItem> curveCom)
    {
        CurveModelItemCom = curveCom.TransferOwnership();
    }

    /// <summary>
    /// Unique name of the curve
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Feed zones on the curve
    /// </summary>
    private ObservableCollection<FeedZoneItem> FeedZones { get; set; } = [];

    /// <inheritdoc />
    public void Dispose()
    {
        CurveModelItemCom.Dispose();
    }

    /// <summary>
    /// Reload feed zones from the curve
    /// </summary>
    /// <param name="dataGrid">UI data grid to refresh</param>
    public void ReloadFeedZones(DataGrid dataGrid)
    {
        // read feed zones
        using var feedZonesCom = CurveModelItemCom.FeedPoints();
        var feedZoneItems = Enumerable
            .Range(0, feedZonesCom.Count())
            .Select(i => ReadFeedZone(feedZonesCom, i)).ToList();

        // refresh UI
        FeedZones.Clear();
        foreach (var feedZoneItem in feedZoneItems)
            FeedZones.Add(feedZoneItem);
        dataGrid.ItemsSource = null;
        dataGrid.ItemsSource = FeedZones;
    }
    
    private FeedZoneItem ReadFeedZone(ComWrapper<ICamApiFeedPointList> feedZonesCom, int index)
    {
        return new FeedZoneItem
        {
            Index = index,
            Length = feedZonesCom.GetLength(index),
            FeedType = feedZonesCom.GetFeedType(index),
            Percentage = feedZonesCom.GetFeedRatePercentage(index),
            ChangeMode = feedZonesCom.GetFeedRateChangeType(index)
        };
    }

    /// <summary>
    /// Add a new feed zone to the curve
    /// </summary>
    public void AddFeedZone()
    {
        CurveModelItemCom.SetUseCustomVectors(true);
        using var curveCom = CurveModelItemCom.Curve();
        var pos = curveCom.GetPoint(0);
        
        // add feed zone
        using var feedZonesCom = CurveModelItemCom.FeedPoints();
        var index = feedZonesCom.AddFeedPoint(pos, 100);
        
        // refresh UI
        FeedZones.Add(ReadFeedZone(feedZonesCom, index));
    }

    /// <summary>
    /// Delete the selected feed zone from the curve
    /// </summary>
    /// <param name="dataGrid">UI data grid to get selection</param>
    public void DeleteSelectedFeedZone(DataGrid dataGrid)
    {
        // get selected
        if (dataGrid.SelectedItem is not FeedZoneItem selectedFeedZone)
            return;

        var index = FeedZones.IndexOf(selectedFeedZone);
        if (index < 0)
            return;
        
        // delete
        using var feedZonesCom = CurveModelItemCom.FeedPoints();
        feedZonesCom.RemoveFeedPoint(index);
        
        // refresh UI
        FeedZones.RemoveAt(index);
    }
}
