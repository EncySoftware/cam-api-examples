using STCustomPropTypes;

namespace ExtensionUtilityDialogWindowNet;

/// <summary>
/// Delegate to fill double value, reading from UI
/// </summary>
public delegate void OnSetDoubleValue(double value);

/// <summary>
/// Wrapper for delegate to fill double value, reading from UI
/// </summary>
public class DoubleValueSetter : IDoubleValueSetter
{
    private readonly OnSetDoubleValue _onSetDoubleValue;
    
    /// <summary>
    /// Wrapper for delegate to fill double value, reading from UI
    /// </summary>
    public DoubleValueSetter(OnSetDoubleValue setter)
    {
        _onSetDoubleValue = setter;
    }
    
    /// <summary>
    /// Execute delegate to put double value
    /// </summary>
    public void SetValue(double value)
    {
        _onSetDoubleValue(value);
    }
}