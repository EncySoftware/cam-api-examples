using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

namespace OperationUserParametersNet;

/// <summary>
/// Single utility that opens a window with Get / Add / Delete buttons operating on the
/// user parameters of the currently selected technology operation.
/// </summary>
public class OperationUserParametersExtension : IExtension, IExtensionUtility, IExtensionLazyUnloadable
{
    private readonly ExtensionWindowLazyUnloadable _windowManager = new();

    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>Allow unloading only when the window is closed.</summary>
    public bool CanUnload
    {
        get => _windowManager.CanUnload;
        set { }
    }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            // Ownership of the application wrapper transfers to the window, which navigates to
            // the current operation on every button click and disposes the wrapper on close.
            // Do NOT wrap it in 'using' here: Run returns before the user clicks anything.
            var applicationCom = ComWrapper.Create(context.CamApplication);
            _windowManager.SetOwnerHandle(applicationCom);
            _windowManager.ShowWindow(() => new UserParametersWindow(applicationCom));
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
