using System.Diagnostics;
using System.Runtime.InteropServices;
using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.Project;
using CAMAPI.ResultStatus;
using CAMAPI.ToolsList;

namespace ExtensionUtilityProjectToolsListNet;

/// <summary>
/// Utility to get information about machining tools list from the active project
/// </summary>
public class ExtensionProjectProjectToolsList: IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            using var applicationCom = new ApiComObject<ICamApiApplication>(context.CamApplication);
            var application = applicationCom.Instance;
            
            // active project
            using var activeProjectCom = new ApiComObject<ICamApiProject>(application.GetActiveProject(out resultStatus));
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Can't get active project: " + resultStatus.Description);
            if (activeProjectCom == null)
                throw new Exception("Active project is not found");
            var activeProject = activeProjectCom.Instance;
            
            var tmpFileName = Path.GetTempFileName();
            File.AppendAllText(tmpFileName,
                "Current project: " + activeProject.FilePath + Environment.NewLine +
                "Tools of the project:" + Environment.NewLine
            );
            
            // tools list
            using var toolsListCom = new ApiComObject<ICamApiMachiningToolsList>(activeProject.ToolsList);
            var toolsList = toolsListCom.Instance;
            for (var i = 0; i < toolsList.Count; i++)
            {
                using var toolInfoCom = new ApiComObject<ICamApiMachiningToolInfo>(toolsList.ToolInfo[i]);
                var toolInfo = toolInfoCom.Instance;
                File.AppendAllText(tmpFileName,
                    "    Tool caption: " + toolInfo.ToolCaption + Environment.NewLine +
                    "        Tool type: " + toolInfo.ToolType + Environment.NewLine +
                    "        Tool GUID: " + toolInfo.ToolGUID + Environment.NewLine +
                    "        Tool ID: " + toolInfo.ToolID + Environment.NewLine +
                    "        Tool number: " + toolInfo.ToolNumber + Environment.NewLine +
                    "        First corrector number: " + toolInfo.FirstCorrectorNumber + Environment.NewLine +
                    "        Connector ID: " + toolInfo.ConnectorID + Environment.NewLine +
                    "        Magazine number: " + toolInfo.MagazineNumber + Environment.NewLine
                );

                using var operationCom = new ApiComObject<ICamApiMachiningToolOperationsIterator>(toolsList.GetOperationsUsingTheTool(toolInfo.ToolID));
                var operations = operationCom.Instance;
                
                operations.Reset();
                if (operations.CurrentOperationIsEmpty())
                    continue;
                File.AppendAllText(tmpFileName, "        Operations using the tool: " + Environment.NewLine);
                while (!operations.CurrentOperationIsEmpty())
                {
                    File.AppendAllText(tmpFileName, "               " + operations.GetCurrentOperationCaption() + Environment.NewLine);
                    operations.MoveNext();
                }
            }

            // show
            Process.Start("notepad.exe", tmpFileName);
        } catch (Exception e) {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
