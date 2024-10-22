using STCustomPropTypes;

namespace ExtensionUtilityDialogWindowNet;

/// <summary>
/// Delegate to get integer value
/// </summary>
public delegate int OnGetIntegerValue();

/// <summary>
/// Wrapper over <see cref="IIntegerValueGetter"/> to hide delegates
/// </summary>
public class IntegerValueGetter : IIntegerValueGetter
{
    private readonly OnGetIntegerValue _onGetIntegerValue;
    
    /// <summary>
    /// Wrapper over <see cref="IIntegerValueGetter"/> to hide delegates
    /// </summary>
    public IntegerValueGetter(OnGetIntegerValue getter)
    {
        _onGetIntegerValue = getter;
    }
    
    /// <summary>
    /// Execute delegate to get Integer value
    /// </summary>
    public int GetValue()
    {
        return _onGetIntegerValue();
    }
}