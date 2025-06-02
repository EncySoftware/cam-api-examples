using STTypes;

namespace AddinImportSvgNet;

/// <summary>
/// Callbacks for SVG path processing
/// </summary>
public class SvgReaderCallbacks
{
    /// <summary>
    /// Called when a new path is started
    /// </summary>
    public Action<string, TST3DPoint>? OnMoveTo;
    
    /// <summary>
    /// Called when a line is drawn
    /// </summary>
    public Action<TST3DPoint>? OnLineTo;
    
    /// <summary>
    /// Called when a path is closed
    /// </summary>
    public Action<bool>? OnClosePath;
    
    /// <summary>
    /// Called when a path is stroked to set line color
    /// </summary>
    public Action<int>? OnSetLineColor { get; set; }
    
    /// <summary>
    /// Called when a path is stroked to set line width
    /// </summary>
    public Action<int>? OnSetLineWidth { get; set; }

    /// <summary>
    /// Called when a circle is drawn
    /// </summary>
    public Action<string, double, double, double>? OnCircleTo { get; set; }

    /// <summary>
    /// Called when an ellipse is drawn
    /// </summary>
    public Action<string, double, double, double, double, double>? OnEllipseTo { get; set; }
}