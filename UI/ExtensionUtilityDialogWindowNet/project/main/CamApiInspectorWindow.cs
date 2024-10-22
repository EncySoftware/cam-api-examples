using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.UIDialogs;
using STCustomPropTypes;

namespace ExtensionUtilityDialogWindowNet;

/// <summary>
/// Wrapper over <see cref="ICAMAPI_UIDialogWindow"/> to hide delegates
/// and make it easier to use
/// </summary>
public class CamApiInspectorWindow
{
    /// <summary>
    /// Inspector window, we are wrapping
    /// </summary>
    private readonly ICAMAPI_UIDialogWindow _inspectorWindow;

    private InspectorItems _items;

    private readonly IST_SimplePropIterator _iterator;
    
    /// <summary>
    /// Create instance, when we have access to extension manager. He already
    /// knows how to create instances of <see cref="ICAMAPI_UIDialogWindow"/>
    /// </summary>
    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
    public CamApiInspectorWindow(IExtensionManager extensionManager)
    {
        // create inspector window
        var extension = extensionManager.GetSingletonExtension("Extension.UIDialogs.Core", out var resultStatus)
                        ?? throw new Exception("Extension.UIDialogs.Core not found");
        if (resultStatus.Code == TResultStatusCode.rsError)
            throw new Exception($"Error getting Extension.UIDialogs.Core: {resultStatus.Description}");
        if (extension is not ICAMAPI_UIDialogsHelper inspectorWindowHelper)
            throw new Exception("Extension is not of type ICAMAPI_UIDialogWindow");
        _inspectorWindow = inspectorWindowHelper.CreateWindow("");
        
        // create helper to fill inspector window
        extension = extensionManager.GetSingletonExtension("Extension.CustomPropHelpers", out resultStatus)
                    ?? throw new Exception("Extension.CustomPropHelpers not found");
        if (resultStatus.Code == TResultStatusCode.rsError)
            throw new Exception($"Error getting Extension.CustomPropHelpers: {resultStatus.Description}");
        var inspectorHelper = extension as IST_CustomPropHelpers
            ?? throw new Exception("Extension is not of type IST_CustomPropHelpers");

        // create empty items list, so we will fill it later
        _iterator = inspectorHelper.CreateSimplePropIterator();
        _items = new InspectorItems(-1, inspectorHelper, _iterator);
    }

    /// <summary>
    /// Title of the window
    /// </summary>
    public string Caption
    {
        get => _inspectorWindow.WindowCaption;
        set => _inspectorWindow.WindowCaption = value;
    }
    
    /// <summary>
    /// Release COM objects - mandatory to call
    /// </summary>
    public void Clear()
    {
        Marshal.ReleaseComObject(_iterator);
        Marshal.ReleaseComObject(_inspectorWindow);
    }
    
    /// <summary>
    /// Set buttons to inspector. Use <see cref="TUIButtonType"/>, cast to ushort, to set buttons
    /// </summary>
    /// <param name="buttons"></param>
    public void SetButtons(ushort buttons)
    {
        _inspectorWindow.Buttons = buttons;
    }
    
    /// <summary>
    /// Show inspector window
    /// </summary>
    /// <returns>Button, user has choosen</returns>
    public TUIButtonType Show()
    {
        _iterator.MoveToRoot();
        if (_iterator is not IST_CustomPropIterator propIterator)
            throw new Exception("Failed to create IST_CustomPropIterator");
        _inspectorWindow.PropIterator = propIterator;
        return _inspectorWindow.ShowModal();
    }
    
    /// <summary>
    /// Add string property to inspector
    /// </summary>
    /// <returns>Index of item, injected into inspector</returns>
    public InspectorItems AddStringProperty(string caption, OnGetStringValue onGetStringValue, OnSetStringValue onSetStringValue)
    {
        _items = _items.AddStringProperty(caption, onGetStringValue, onSetStringValue);
        return _items;
    }
    
    /// <summary>
    /// Add integer property to inspector
    /// </summary>
    /// <returns>Index of item, injected into inspector</returns>
    public InspectorItems AddIntegerProperty(string caption, OnGetIntegerValue onGetIntegerValue, OnSetIntegerValue onSetIntegerValue)
    {
        _items = _items.AddIntegerProperty(caption, onGetIntegerValue, onSetIntegerValue);
        return _items;
    }
    
    /// <summary>
    /// Add double property to inspector
    /// </summary>
    /// <returns>Index of item, injected into inspector</returns>
    public InspectorItems AddDoubleProperty(string caption, OnGetDoubleValue onGetDoubleValue, OnSetDoubleValue onSetDoubleValue)
    {
        _items = _items.AddDoubleProperty(caption, onGetDoubleValue, onSetDoubleValue);
        return _items;
    }
    
    /// <summary>
    /// Add boolean property to inspector
    /// </summary>
    /// <returns>Index of item, injected into inspector</returns>
    public InspectorItems AddBooleanProperty(string caption, OnGetBooleanValue onGetBooleanValue, OnSetBooleanValue onSetBooleanValue)
    {
        _items = _items.AddBooleanProperty(caption, onGetBooleanValue, onSetBooleanValue);
        return _items;
    }
}