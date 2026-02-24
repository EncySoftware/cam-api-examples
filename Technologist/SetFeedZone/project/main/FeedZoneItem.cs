using System.ComponentModel;
using System.Runtime.CompilerServices;
using CAMAPI.ModelFormerTypes;

namespace SetFeedZone;

/// <summary>
/// Feed zone item for setting feed rate
/// </summary>
public class FeedZoneItem
{
    /// <summary>
    /// Percentage of feed rate on the zone
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// Feed type on the zone
    /// </summary>
    public TModelFormerFeedType FeedType { get; set; }
    
    /// <summary>
    /// Change mode of feed rate on the zone
    /// </summary>
    public TModelFormerFeedRateChangeType ChangeMode { get; set; }
    
    /// <summary>
    /// Length of the zone
    /// </summary>
    public double Length { get; set; }
    
    /// <summary>
    /// Index of the zone
    /// </summary>
    public int Index { get; set; }
}