using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.TechnologyForm;

namespace ExtensionOperationPopupOnChangeNet;

/// <summary>
/// Utility to get information about machine from the active project
/// </summary>
public class ExtensionOperationPopupOnChange: IExtension, IExtensionOperationPopup
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
            popupItems.AddItem("Catch operation tool change",
                "Catch operation tool change",
                true,
                new CatchOperationChanged(),
                out resultStatus);
        }
        catch (Exception ex)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = ex.Message;
        }
    }
}
