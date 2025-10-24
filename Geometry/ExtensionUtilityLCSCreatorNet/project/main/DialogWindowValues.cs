namespace ExtensionUtilityLCSCreatorNet;

/// <summary>
/// Simple class to store values from dialog window
/// </summary>
public class DialogWindowValues
{
    /// <summary>
    /// Some string value
    /// </summary>
    public string StringValue { get; set; } = "New_LCS";
    /// <summary>
    /// Some enum value
    /// </summary>
    public string EnumIdValue { get; set; } = "Global CS";
    /// <summary>
    /// Some x value
    /// </summary>
    public double DoubleXValue { get; set; } = 0;
    /// <summary>
    /// Some y value
    /// </summary>
    public double DoubleYValue { get; set; } = 0;
    /// <summary>
    /// Some z value
    /// </summary>
    public double DoubleZValue { get; set; } = 0;

    /// <summary>
    /// Some Rx value
    /// </summary>
    public double DoubleRXValue { get; set; } = 0;
    /// <summary>
    /// Some Ry value
    /// </summary>
    public double DoubleRYValue { get; set; } = 0;
    /// <summary>
    /// Some Rz value
    /// </summary>
    public double DoubleRZValue { get; set; } = 0;
}