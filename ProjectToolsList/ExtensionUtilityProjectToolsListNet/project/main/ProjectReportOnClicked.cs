using System.Diagnostics;
using CAMAPI.ApplicationMainForm;
using CAMAPI.DotnetHelper;
using CAMAPI.ResultStatus;

namespace ExtensionUtilityProjectToolsListNet;

/// <summary>
/// Show information about the active project tool`s list
/// </summary>
public class ProjectReportOnClicked : ICamApiProjectReportPopupItemOnClicked
{
    /// <summary>
    /// Show project tool`s list info in the notepad 
    /// </summary>
    public void OnItemClicked(IExtensionProjectReportPopupItemOnClickedContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;

        try
        {
            // active project
            using var projectCom = ComWrapper.Create(context.ActiveProject);
            
            var tmpFileName = Path.GetTempFileName();
            File.AppendAllText(tmpFileName,
                "Current project: " + projectCom.FilePath() + Environment.NewLine +
                "Tools of the project:" + Environment.NewLine
            );
            
            // tools list
            using var toolsListCom = projectCom.ToolsList();
            for (var i = 0; i < toolsListCom.Count(); i++)
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

            // show
            Process.Start("notepad.exe", tmpFileName);
        }
        catch (Exception ex)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = ex.Message;
        }
    }
}
