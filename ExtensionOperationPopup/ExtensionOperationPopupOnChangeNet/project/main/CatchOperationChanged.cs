using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.EventHandler;
using CAMAPI.ResultStatus;
using CAMAPI.TechnologyForm;
using CAMAPI.TechOperation;
using CAMAPI.Tools;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;

namespace ExtensionOperationPopupOnChangeNet;

/// <summary>
/// Show information about current tool after changing it
/// </summary>
public class CatchOperationChanged : ICamApiTechnologyFormOperationPopupItemOnClicked,
    ICamApiEventHandler,
    ICamApiHandlerTechOperationToolChanged,
    IDisposable
{
    /// <summary>
    /// Flag to check if listener is registered
    /// </summary>
    private bool _isRegistered;

    /// <summary>
    /// Current operation. We shouldn't store it as a field, because in this case it won't be
    /// disposed properly
    /// </summary>
    private static ComWrapper<ICamApiTechOperation> GetOperation()
    {
        using var applicationSingletonCom =
            SystemExtensionFactory.GetSingletonExtension<ICamApiApplicationSingleton>("Extension.Global.Singletons.Application");
        var applicationSingleton = applicationSingletonCom.Instance
                                  ?? throw new NullReferenceException("Failed to create application singleton object");
        return new ComWrapper<ICamApiTechOperation>(applicationSingleton.GetCurrentOperation(out _));
    }
    
    /// <summary>
    /// Register/Unregister listener for tool change
    /// </summary>
    public void OnItemClicked(IExtensionOperationPopupItemOnClickedContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            if (_isRegistered)
                return;
            
            using var operationCom = GetOperation();
            operationCom.Instance?.RegisterHandler("CatchOperationChanged", this, new ListString(), out resultStatus);
            _isRegistered = true;
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }

    /// <summary>
    /// Unregister listener for tool change and dispose all COM objects
    /// </summary>
    public void Dispose()
    {
        using var operationCom = GetOperation();
        operationCom.Instance?.UnregisterHandler("CatchOperationChanged", out _);
    }

    /// <summary>
    /// Always return false - only synchronous mode is supported
    /// </summary>
    public bool GetAsyncMode(string interfaceUid)
    {
        return false;
    }

    /// <summary>
    /// Show information about current tool after changing
    /// </summary>
    public void ToolChanged(string handlerIdent, ICamApiMachiningTool tool)
    {
        // current operation
        using var operationCom = GetOperation();
        var operation = operationCom.Instance
                        ?? throw new NullReferenceException("Failed to create operation object");
        
        // show message
        using var toolCom = ComWrapper.Create(tool);
        var toolObj = toolCom.Instance
                      ?? throw new NullReferenceException("Failed to create tool object");
        var message = $"For operation {operation.FullName} tool changed to {toolObj.ToolName}";
            
        using var helperCom = UIDialogs.CreateHelper();
        var helper = helperCom.Instance
                     ?? throw new Exception("Failed to create UIDialogs helper");
        var buttons = MessageBoxHelper.BuildButtons(TUIButtonType.btOk);

        helper.MessageBox(message, TMessageDialogType.mdtInformation, buttons, TUIButtonType.btOk, "Tool changed");
    }
}