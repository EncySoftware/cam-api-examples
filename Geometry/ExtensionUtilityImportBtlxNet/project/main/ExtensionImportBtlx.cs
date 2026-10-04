using CAMAPI.Application;
using CAMAPI.ApplicationMainForm;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;

namespace ExtensionUtilityImportBtlxNet;

/// <summary>
/// Utility that asks for a BTLx file, imports its parts into the active project and shows how
/// many of them came in
/// </summary>
public class ExtensionImportBtlx : IExtension, IExtensionUtility
{
    private const string Title = "BTLx import";

    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            using var dialogsCom = UIDialogs.CreateHelper();

            var filePath = dialogsCom.Invoke(dialogs =>
                dialogs.SelectFileDialog("Select a BTLx file", "BTLx (*.btlx)|*.btlx|All files (*.*)|*.*", ""));
            if (string.IsNullOrEmpty(filePath))
                return;

            var summary = Import(context, filePath);

            dialogsCom.Invoke(dialogs => dialogs.MessageBox(summary.BuildReport(), TMessageDialogType.mdtInformation,
                (ushort)TUIButtonTypeFlags.btfOk, TUIButtonType.btOk, Title));
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }

    /// <summary>
    /// Import the file into the active project, keeping the main window from redrawing meanwhile
    /// </summary>
    private static ImportSummary Import(IExtensionUtilityContext context, string filePath)
    {
        using var applicationCom = ComWrapper.Create(context.CamApplication);
        using var mainFormCom = applicationCom.MainForm();

        mainFormCom.Invoke(mainForm => mainForm.BeginFreeze((ushort)TFreezeInterfaceType.afiiGeneral));
        try
        {
            using var projectCom = applicationCom.GetActiveProject();
            return BtlxImporter.Import(projectCom, filePath);
        }
        finally
        {
            mainFormCom.Invoke(mainForm => mainForm.EndFreeze());
        }
    }
}
