using STCustomPropTypes;

namespace ExtensionUtilityDialogWindowNet;

/// <summary>
/// Delegate to get bool value
/// </summary>
public delegate bool OnGetBooleanValue();

/// <summary>
/// Wrapper over <see cref="IBooleanValueGetter"/> to hide delegates
/// </summary>
public class BooleanValueGetter : IBooleanValueGetter
{
    private readonly OnGetBooleanValue _onGetBooleanValue;
    
    /// <summary>
    /// Wrapper over <see cref="IBooleanValueGetter"/> to hide delegates
    /// </summary>
    public BooleanValueGetter(OnGetBooleanValue getter)
    {
        _onGetBooleanValue = getter;
    }
    
    /// <summary>
    /// Execute delegate to get Boolean value
    /// </summary>
    public bool GetValue()
    {
        return _onGetBooleanValue();
    }
}