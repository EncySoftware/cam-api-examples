using CAMAPI.ApplicationMainForm;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

namespace ExtensionUtilityProjectToolsListNet;

/// <summary>
/// Extension to demonstrate entry point "project_report_popup"
/// </summary>
public class ExtensionProjectReportPopup : IExtension, IExtensionProjectReportPopup
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Add a popup item that shows project tools list information
    /// </summary>
    /// <param name="context">Context containing the active project and the popup items collection</param>
    /// <param name="resultStatus">Object to contain some message to calling code</param>
    public void Build(IExtensionProjectReportPopupBuildContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;

        try
        {
            var popupItems = context.ReportPopup;
            popupItems.AddItem(
                "ShowProjectToolsInfo", "Show project tools list", 
                true, new ProjectReportOnClicked(), out resultStatus);
        }
        catch (Exception ex)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = ex.Message;
        }
    }
}
