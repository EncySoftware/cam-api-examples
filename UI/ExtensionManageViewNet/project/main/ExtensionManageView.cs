using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

namespace ExtensionManageViewNet;

/// <summary>
/// Utility to manage the main window view
/// </summary>
public class ExtensionManageView: IExtension, IExtensionUtility, IExtensionLazyUnloadable
{
    private bool _canUnload;

    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }
    
    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            // arrange
            using var applicationCom = ComWrapper.Create(context.CamApplication);
            using var applicationMainFormCom = applicationCom.MainForm();
            var mainWindowHandle = applicationMainFormCom.MainWindowHandle();
            var viewPortCom = applicationMainFormCom.MainViewPort();
            
            // show form
            WindowHelper.ShowStaWindow(mainWindowHandle, 
                () => new ViewControlWindow(viewPortCom),
                () => _canUnload = true);
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }

    
    /// <summary>
    /// Allow unloading only when a window is closed
    /// </summary>
    public bool CanUnload
    {
        get => _canUnload;
        set { }
    }
}
