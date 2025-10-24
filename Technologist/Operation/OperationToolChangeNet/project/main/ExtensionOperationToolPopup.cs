using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.Singletons;
using CAMAPI.TechnologyForm;

namespace ExtensionOperationToolPopupNet;

/// <summary>
/// Extension to demonstrate entry point "operation_popup" 
/// </summary>
public class ExtensionOperationToolPopup : IExtension, IExtensionOperationPopup
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Create example.nc in temp folder with CLData for selected operation
    /// </summary>
    /// <param name="context">Running main application info</param>
    /// <param name="resultStatus">Object to contain some message to calling code</param>
    public void Build(IExtensionOperationPopupBuildContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;

        try
        {
            var popupItems = context.OperationPopup;
            popupItems.AddItem("Select tool for operation", "Select tool for operation", true, new SelectToolOnClicked(), out resultStatus);
        }
        catch (Exception ex)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = ex.Message;
        }
    }
}