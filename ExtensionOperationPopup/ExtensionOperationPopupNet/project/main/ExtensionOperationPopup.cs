using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.TechnologyForm;

namespace ExtensionOperationPopupNet;

/// <summary>
/// Extension to demonstrate entry point "operation_popup" 
/// </summary>
public class ExtensionOperationPopup : IExtension, IExtensionOperationPopup
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
            popupItems.AddItem("Calculate NC", "Calculate NC", true, new MakeNcOnClicked(), out resultStatus);
        }
        catch (Exception ex)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = ex.Message;
        }
    }
}