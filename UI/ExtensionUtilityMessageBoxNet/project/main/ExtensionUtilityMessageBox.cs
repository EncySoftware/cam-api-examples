using System.Runtime.InteropServices;
using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;

namespace ExtensionUtilityMessageBoxNet;

/// <summary>
/// Utility to show simple message box
/// </summary>
public class ExtensionUtilityMessageBox: IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        using var helperCom = UIDialogs.CreateComObject();
        var helper = helperCom.Instance
            ?? throw new Exception("Failed to create UIDialogs helper");
        var buttons = MessageBoxHelper.BuildButtons(TUIButtonType.btOk, TUIButtonType.btCancel);
        helper.MessageBox("Some text", TMessageDialogType.mdtInformation, buttons, TUIButtonType.btOk, "Some text");
        resultStatus = default;
    }
}
