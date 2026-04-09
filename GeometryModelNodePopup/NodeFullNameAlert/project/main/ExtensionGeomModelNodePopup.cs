using CAMAPI.Extensions;
using CAMAPI.GeometryModelForm;
using CAMAPI.ResultStatus;

namespace NodeFullNameAlert;

/// <summary>
/// Extension to demonstrate entry point "geom_model_node_popup"
/// </summary>
public class ExtensionGeomModelNodePopup : IExtension, IExtensionGeomModelNodePopup
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Add a popup item that shows the full name of the selected geometry tree node
    /// </summary>
    /// <param name="context">Context containing the selected node and the popup items collection</param>
    /// <param name="resultStatus">Object to contain some message to calling code</param>
    public void Build(IExtensionGeomModelNodePopupBuildContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;

        try
        {
            var popupItems = context.NodePopup;
            popupItems.AddItem("ShowNodeFullName", "Show node full name", true, new NodeFullNameAlertOnClicked(), out resultStatus);
        }
        catch (Exception ex)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = ex.Message;
        }
    }
}
