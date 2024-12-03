namespace ExtensionUtilityDialogWindowNet;

/// <summary>
/// Simple class to store values from dialog window
/// </summary>
public class DialogWindowValues
{
    /// <summary>
    /// Some enum value
    /// </summary>
    public int EnumIndexedValue { get; set; }
    
    /// <summary>
    /// Some enum value
    /// </summary>
    public string EnumIdValue { get; set; } = "";
    
    /// <summary>
    /// Some string value
    /// </summary>
    public string StringValue { get; set; } = "";

    /// <summary>
    /// Some sub string value
    /// </summary>
    public string SubStringValue { get; set; } = "";
}