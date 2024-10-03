using STCustomPropTypes;

namespace ExtensionUtilityDialogWindowNet;

/// <summary>
/// Delegate to fill integer value, reading from UI
/// </summary>
public delegate void OnSetIntegerValue(int value);

/// <summary>
/// Wrapper for delegate to fill integer value, reading from UI
/// </summary>
public class IntegerValueSetter : IIntegerValueSetter
{
    private readonly OnSetIntegerValue _onSetIntegerValue;
    
    /// <summary>
    /// Wrapper for delegate to fill int value, reading from UI
    /// </summary>
    public IntegerValueSetter(OnSetIntegerValue setter)
    {
        _onSetIntegerValue = setter;
    }
    
    /// <summary>
    /// Execute delegate to put int value
    /// </summary>
    public void SetValue(int value)
    {
        _onSetIntegerValue(value);
    }
}