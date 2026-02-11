using System.Windows;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.ViewPort;

namespace ExtensionManageViewNet;

/// <summary>
/// Utility to manage the main window view
/// </summary>
public class ExtensionManageView: IExtension, IExtensionUtility, IExtensionLazyUnloadable
{
    private long _mainWindowHandle;
    private bool _canUnload;

    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }
    
    private void ShowForm(long ownerHandle, ComWrapper<ICamApiViewPort> viewPortCom)
    {
        var thread = new Thread(() =>
        {
            try
            {
                var window = new ViewControlWindow(viewPortCom);
                var helper = new System.Windows.Interop.WindowInteropHelper(window);
                helper.Owner = (IntPtr)ownerHandle;
                window.Closed += (s, e) => 
                {
                    window.Dispose();
                    _canUnload = true;
                };
                window.Show();
                System.Windows.Threading.Dispatcher.Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
    }
    
    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            // arrange
            using var applicationCom = ComWrapper.Create(context.CamApplication);
            using var applicationMainFormCom = applicationCom.MainForm();
            _mainWindowHandle = applicationMainFormCom.Invoke(mainForm => mainForm.MainWindowHandle);
            var viewPortCom = applicationMainFormCom.InvokeAndWrap(mainForm => mainForm.MainViewPort);

            // show form
            try
            {
                ShowForm(_mainWindowHandle, viewPortCom);
            }
            catch (Exception)
            {
                viewPortCom.Dispose();
                throw;
            }
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
        set
        {
            
        }
    }
}
