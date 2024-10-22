using CAMAPI.UIDialogs.DotnetHelper;
using STCustomPropTypes;

namespace ExtensionUtilityDialogWindowNet;

/// <summary>
/// List of items, used in inspector. All items has the same parent index,
/// so they are shown in the same group
/// </summary>
public class InspectorItems
{
    /// <summary>
    /// Unique number, that inspector assigns in create moment
    /// </summary>
    private readonly int _parentIndex;

    /// <summary>
    /// Instance to create new fields
    /// </summary>
    private readonly IST_CustomPropHelpers _inspectorHelper;
    
    /// <summary>
    /// Iterator in inspector, to add new fields
    /// </summary>
    private readonly IST_SimplePropIterator _iterator;

    /// <summary>
    /// Create new group of items in inspector
    /// </summary>
    public InspectorItems(int parentIndex,
        IST_CustomPropHelpers inspectorHelper,
        IST_SimplePropIterator iterator)
    {
        _parentIndex = parentIndex;
        _inspectorHelper = inspectorHelper;
        _iterator = iterator;
    }

    /// <summary>
    /// Add child string property to inspector
    /// </summary>
    /// <returns>Index of item, injected into inspector</returns>
    public InspectorItems AddStringProperty(string caption, OnGetStringValue onGetStringValue, OnSetStringValue onSetStringValue)
    {
        var propString = _inspectorHelper.CreateStringProp(caption);
        propString.ValueGetter = new StringValueGetter(onGetStringValue);
        propString.ValueSetter = new StringValueSetter(onSetStringValue);
        var index = _iterator.AddNewProp(propString, _parentIndex);
        return new InspectorItems(index, _inspectorHelper, _iterator);
    }

    /// <summary>
    /// Add child double property to inspector
    /// </summary>
    /// <returns>Index of item, injected into inspector</returns>
    public InspectorItems AddIntegerProperty(string caption, OnGetIntegerValue onGetIntegerValue, OnSetIntegerValue onSetIntegerValue)
    {
        var propInteger = _inspectorHelper.CreateIntegerProp(caption);
        propInteger.ValueGetter = new IntegerValueGetter(onGetIntegerValue);
        propInteger.ValueSetter = new IntegerValueSetter(onSetIntegerValue);
        var index = _iterator.AddNewProp(propInteger, _parentIndex);
        return new InspectorItems(index, _inspectorHelper, _iterator);
    }

    /// <summary>
    /// Add child double property to inspector
    /// </summary>
    /// <returns>Index of item, injected into inspector</returns>
    public InspectorItems AddDoubleProperty(string caption, ExtensionUtilityDialogWindowNet.OnGetDoubleValue onGetDoubleValue, ExtensionUtilityDialogWindowNet.OnSetDoubleValue onSetDoubleValue)
    {
        var propDouble = _inspectorHelper.CreateDoubleProp(caption);
        propDouble.ValueGetter = new ExtensionUtilityDialogWindowNet.DoubleValueGetter(onGetDoubleValue);
        propDouble.ValueSetter = new ExtensionUtilityDialogWindowNet.DoubleValueSetter(onSetDoubleValue);
        var index = _iterator.AddNewProp(propDouble, _parentIndex);
        return new InspectorItems(index, _inspectorHelper, _iterator);
    }

    /// <summary>
    /// Add child boolean property to inspector
    /// </summary>
    /// <returns>Index of item, injected into inspector</returns>
    public InspectorItems AddBooleanProperty(string caption, ExtensionUtilityDialogWindowNet.OnGetBooleanValue onGetBooleanValue, ExtensionUtilityDialogWindowNet.OnSetBooleanValue onSetBooleanValue)
    {
        var propBoolean = _inspectorHelper.CreateBooleanProp(caption);
        propBoolean.ValueGetter = new ExtensionUtilityDialogWindowNet.BooleanValueGetter(onGetBooleanValue);
        propBoolean.ValueSetter = new ExtensionUtilityDialogWindowNet.BooleanValueSetter(onSetBooleanValue);
        var index = _iterator.AddNewProp(propBoolean, _parentIndex);
        return new InspectorItems(index, _inspectorHelper, _iterator);
    }
}