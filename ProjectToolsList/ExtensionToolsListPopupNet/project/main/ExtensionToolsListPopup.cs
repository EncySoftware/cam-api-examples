using System;
using System.Diagnostics;
using System.IO;
using CAMAPI.ApplicationMainForm;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.Project;
using CAMAPI.ResultStatus;

namespace ExtensionToolsListPopupNet;

/// <summary>
/// Extension to demonstrate entry point "tools_list_popup"
/// </summary>
public class ExtensionToolsListPopup : IExtension, IExtensionToolsListPopup
{
    private static readonly string[] ItemCaptions =
    [
        "Show tools list info",
        "Show tools list info 1",
        "Show tools list info 2"
    ];

    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public int ItemsCount => ItemCaptions.Length;

    /// <inheritdoc />
    public string GetItemCaption(int itemIndex) => ItemCaptions[itemIndex];

    /// <inheritdoc />
    public void ExecuteItem(int itemIndex, ICamApiProject activeProject, out TResultStatus resultStatus)
    {
        resultStatus = default;

        try
        {
            using var projectCom = ComWrapper.Create(activeProject);

            var tmpFileName = Path.GetTempFileName();
            File.AppendAllText(tmpFileName,
                "Current project: " + projectCom.FilePath() + Environment.NewLine +
                "Tools of the project:" + Environment.NewLine
            );

            using var toolsListCom = projectCom.ToolsList();
            var toolsList = toolsListCom.Invoke(tools => tools.Count);
            for (var i = 0; i < toolsList; i++)
            {
                using var toolInfoCom = toolsListCom.ToolInfo(i);
                File.AppendAllText(tmpFileName,
                    "    Tool caption: " + toolInfoCom.ToolCaption() + Environment.NewLine +
                    "        Tool type: " + toolInfoCom.ToolType() + Environment.NewLine +
                    "        Tool GUID: " + toolInfoCom.ToolGUID() + Environment.NewLine +
                    "        Tool ID: " + toolInfoCom.ToolID() + Environment.NewLine +
                    "        Tool number: " + toolInfoCom.ToolNumber() + Environment.NewLine +
                    "        First corrector number: " + toolInfoCom.FirstCorrectorNumber() + Environment.NewLine +
                    "        Connector ID: " + toolInfoCom.ConnectorID() + Environment.NewLine +
                    "        Magazine number: " + toolInfoCom.MagazineNumber() + Environment.NewLine
                );

                using var operationCom = toolsListCom.GetOperationsUsingTheTool(toolInfoCom.ToolID());

                operationCom.Reset();
                if (operationCom.CurrentOperationIsEmpty())
                    continue;
                File.AppendAllText(tmpFileName, "        Operations using the tool: " + Environment.NewLine);
                while (!operationCom.CurrentOperationIsEmpty())
                {
                    File.AppendAllText(tmpFileName, "               " + operationCom.GetCurrentOperationCaption() + Environment.NewLine);
                    operationCom.MoveNext();
                }
            }

            Process.Start("notepad.exe", tmpFileName);
        }
        catch (Exception ex)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = ex.Message;
        }
    }
}
