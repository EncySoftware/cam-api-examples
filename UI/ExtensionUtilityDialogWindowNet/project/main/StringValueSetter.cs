using STCustomPropTypes;

namespace ExtensionUtilityDialogWindowNet;

/// <summary>
/// Delegate to fill string value, reading from UI
/// </summary>
public delegate void OnSetStringValue(string value);

/// <summary>
/// Wrapper for delegate to fill string value, reading from UI
/// </summary>
public class StringValueSetter : IStringValueSetter
{
    private readonly OnSetStringValue _onSetStringValue;
    
    /// <summary>
    /// Wrapper for delegate to fill string value, reading from UI
    /// </summary>
    public StringValueSetter(OnSetStringValue setter)
    {
        _onSetStringValue = setter;
    }
    
    /// <summary>
    /// Execute delegate to put string value
    /// </summary>
    public void SetValue(string value)
    {
        _onSetStringValue(value);
    }
}