using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.DotnetHelper;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;

namespace ExtensionMachineKitNet;

/// <summary>
/// Utility that creates a .mKit file through the MachineKit file API and shows what it has written
/// </summary>
public class ExtensionMachineKit : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Creates the kit in the temporary folder and shows the report. Errors return in resultStatus, an exception
    /// never leaves the method
    /// </summary>
    /// <param name="context">Information about current running instance</param>
    /// <param name="resultStatus">Structure to return error</param>
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            var kitPath = Path.Combine(Path.GetTempPath(), "ExtensionMachineKitNet", "Example.mKit");
            var libraryFolder = Path.GetDirectoryName(typeof(ExtensionMachineKit).Assembly.Location)!;
            var modelPath = Path.Combine(libraryFolder, "Models", "tool.stp");
            using var report = new StringWriter();
            using var applicationCom = ComWrapper.Create(context.CamApplication);
            try
            {
                MachineKitExample.Run(applicationCom, kitPath, modelPath, report);
            }
            catch (InvalidOperationException e)
            {
                report.WriteLine($"MachineKit error: {e.Message}");
            }
            ShowReport(report.ToString());
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }

    /// <summary>
    /// Shows the report in an ENCY message box
    /// </summary>
    private static void ShowReport(string report)
    {
        using var helperCom = UIDialogs.CreateHelper();
        var buttons = MessageBoxHelper.BuildButtons(TUIButtonType.btOk);
        helperCom.Invoke(helper =>
        {
            helper.MessageBox(report, TMessageDialogType.mdtInformation, buttons, TUIButtonType.btOk, "MachineKit");
        });
    }
}
