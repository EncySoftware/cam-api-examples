using STCustomPropTypes;

namespace ExtensionUtilityDialogWindowNet;

/// <summary>
/// Delegate to fill bool value, reading from UI
/// </summary>
public delegate void OnSetBooleanValue(bool value);

/// <summary>
/// Wrapper for delegate to fill boolean value, reading from UI
/// </summary>
public class BooleanValueSetter : IBooleanValueSetter
{
    private readonly OnSetBooleanValue _onSetBooleanValue;
    
    /// <summary>
    /// Wrapper for delegate to fill boolean value, reading from UI
    /// </summary>
    public BooleanValueSetter(OnSetBooleanValue setter)
    {
        _onSetBooleanValue = setter;
    }
    
    /// <summary>
    /// Execute delegate to put boolean value
    /// </summary>
    public void SetValue(bool value)
    {
        _onSetBooleanValue(value);
    }
}