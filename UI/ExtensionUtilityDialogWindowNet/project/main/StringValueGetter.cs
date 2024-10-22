using STCustomPropTypes;

namespace ExtensionUtilityDialogWindowNet;

/// <summary>
/// Delegate to get string value
/// </summary>
public delegate string OnGetStringValue();

/// <summary>
/// Wrapper over <see cref="IStringValueGetter"/> to hide delegates
/// </summary>
public class StringValueGetter : IStringValueGetter
{
    private readonly OnGetStringValue _onGetStringValue;
    
    /// <summary>
    /// Wrapper over <see cref="IStringValueGetter"/> to hide delegates
    /// </summary>
    public StringValueGetter(OnGetStringValue getter)
    {
        _onGetStringValue = getter;
    }
    
    /// <summary>
    /// Execute delegate to get string value
    /// </summary>
    public string GetValue()
    {
        return _onGetStringValue();
    }
}