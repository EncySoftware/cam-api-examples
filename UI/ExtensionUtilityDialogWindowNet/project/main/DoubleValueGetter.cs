using STCustomPropTypes;

namespace ExtensionUtilityDialogWindowNet;

/// <summary>
/// Delegate to get double value
/// </summary>
public delegate double OnGetDoubleValue();

/// <summary>
/// Wrapper over <see cref="IDoubleValueGetter"/> to hide delegates
/// </summary>
public class DoubleValueGetter : IDoubleValueGetter
{
    private readonly OnGetDoubleValue _onGetDoubleValue;
    
    /// <summary>
    /// Wrapper over <see cref="IDoubleValueGetter"/> to hide delegates
    /// </summary>
    public DoubleValueGetter(OnGetDoubleValue getter)
    {
        _onGetDoubleValue = getter;
    }
    
    /// <summary>
    /// Execute delegate to get double value
    /// </summary>
    public double GetValue()
    {
        return _onGetDoubleValue();
    }
}