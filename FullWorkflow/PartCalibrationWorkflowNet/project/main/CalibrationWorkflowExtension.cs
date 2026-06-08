using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using PartCalibrationWorkflowNet.Service;

namespace PartCalibrationWorkflowNet;

/// <summary>
/// Single entry point for the full calibration workflow.
/// Opens a non-modal tabbed window that keeps the project interactive while guiding
/// the user through: project setup → NC generation → machine simulation → calibration.
///
/// Implements <see cref="IExtensionLazyUnloadable"/> so ENCY defers DLL unloading
/// until the window is closed.
/// </summary>
public class CalibrationWorkflowExtension : IExtension, IExtensionUtility, IExtensionLazyUnloadable
{
    private readonly ExtensionWindowLazyUnloadable _windowManager = new();

    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public bool CanUnload
    {
        get => _windowManager.CanUnload;
        set { }
    }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        DbgLog.Write("Run entered");
        try
        {
            using var applicationCom = ComWrapper.Create(context.CamApplication);
            _windowManager.SetOwnerHandle(applicationCom);
            DbgLog.Write("Owner handle set");

            // Second wrapper — no 'using' — ownership transferred to the window,
            // which disposes it in CalibrationWorkflowWindow.Dispose().
            var appForWindow = ComWrapper.Create(context.CamApplication);
            _windowManager.ShowWindow(() =>
            {
                DbgLog.Write("ShowWindow factory invoked on STA thread");
                return new CalibrationWorkflowWindow(appForWindow);
            });
            DbgLog.Write("ShowWindow returned");
            // Run returns immediately; the window lives on its own STA thread.
        }
        catch (Exception e)
        {
            DbgLog.Write("Run failed", e);
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
