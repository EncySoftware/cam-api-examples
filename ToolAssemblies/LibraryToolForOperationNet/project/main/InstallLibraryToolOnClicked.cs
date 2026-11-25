using CAMAPI.DotnetHelper;
using CAMAPI.ResultStatus;
using CAMAPI.TechnologyForm;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;

namespace LibraryToolForOperationNet;

/// <summary>
/// Installs a mill from the component libraries as the tool of the selected operation and shows the report
/// </summary>
public class InstallLibraryToolOnClicked : ICamApiTechnologyFormOperationPopupItemOnClicked
{
    /// <summary>
    /// Runs the example on the selected operation of the active project. Errors go to the report, an exception
    /// never leaves the method
    /// </summary>
    /// <param name="context">Selected operation, active project and the clicked item</param>
    /// <param name="resultStatus">Structure to return error</param>
    public void OnItemClicked(IExtensionOperationPopupItemOnClickedContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            using var report = new StringWriter();
            var failed = false;
            using (var projectCom = ComWrapper.Create(context.ActiveProject))
            using (var operationCom = ComWrapper.Create(context.SelectedOperation))
            {
                try
                {
                    LibraryToolForOperation.Run(projectCom, operationCom, report);
                }
                catch (Exception e)
                {
                    failed = true;
                    report.WriteLine($"Error: {e.Message}");
                }
            }
            ShowReport(report.ToString(), failed);
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
    private static void ShowReport(string report, bool failed)
    {
        using var helperCom = UIDialogs.CreateHelper();
        var buttons = MessageBoxHelper.BuildButtons(TUIButtonType.btOk);
        var type = failed ? TMessageDialogType.mdtWarning : TMessageDialogType.mdtInformation;
        helperCom.Invoke(helper =>
        {
            helper.MessageBox(report, type, buttons, TUIButtonType.btOk, "Library tool for the operation");
        });
    }
}
