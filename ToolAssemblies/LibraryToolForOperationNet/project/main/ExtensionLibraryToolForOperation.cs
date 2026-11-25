using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.TechnologyForm;

namespace LibraryToolForOperationNet;

/// <summary>
/// Operation popup extension: its item installs a mill from the component libraries as the tool of the operation
/// </summary>
public class ExtensionLibraryToolForOperation : IExtension, IExtensionOperationPopup
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Adds the item to the popup menu of the operation
    /// </summary>
    /// <param name="context">Popup menu, selected operation and active project</param>
    /// <param name="resultStatus">Structure to return error</param>
    public void Build(IExtensionOperationPopupBuildContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            using var popupCom = ComWrapper.Create(context.OperationPopup);
            popupCom.Invoke(popup =>
            {
                popup.AddItem("InstallLibraryMill", $"Install a library mill D{LibraryToolForOperation.Diameter}",
                    true, new InstallLibraryToolOnClicked(), out var status);
                return status;
            });
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
