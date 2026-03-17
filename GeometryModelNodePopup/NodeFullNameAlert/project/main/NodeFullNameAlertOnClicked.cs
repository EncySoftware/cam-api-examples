using CAMAPI.DotnetHelper;
using CAMAPI.GeometryModelForm;
using CAMAPI.ResultStatus;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;

namespace NodeFullNameAlert;

/// <summary>
/// Show full name of the clicked geometry tree node
/// </summary>
public class NodeFullNameAlertOnClicked : ICamApiGeomModelNodePopupItemOnClicked
{
    /// <summary>
    /// Show a message box with the full name of the selected geometry tree node
    /// </summary>
    public void OnItemClicked(IExtensionGeomModelNodePopupItemOnClickedContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;

        try
        {
            using var selectedNodeCom = ComWrapper.Create(context.SelectedNode);
            if (selectedNodeCom.IsNull)
                throw new Exception("Selected node is null");

            using var helperCom = UIDialogs.CreateHelper();
            var helper = helperCom.Instance
                         ?? throw new Exception("Failed to create UIDialogs helper");
            var buttons = MessageBoxHelper.BuildButtons(TUIButtonType.btOk);
            helper.MessageBox("Node full name: " + selectedNodeCom.FullName(), TMessageDialogType.mdtInformation, buttons, TUIButtonType.btOk, "NodeFullNameAlert");
        }
        catch (Exception ex)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = ex.Message;
        }
    }
}
