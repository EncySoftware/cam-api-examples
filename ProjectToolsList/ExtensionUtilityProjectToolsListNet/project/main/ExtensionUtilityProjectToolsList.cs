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
        ICamApiProject? activeProject = null;
        ICamApiMachiningToolsList? toolsList = null;
        ICamApiMachiningToolInfo? toolInfo = null;
        ICamApiMachiningToolOperationsIterator? operations = null;
        try
        {
            // active project
            activeProject = context.CamApplication.GetActiveProject(out resultStatus);
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Can't get active project: " + resultStatus.Description);
            if (activeProject == null)
                throw new Exception("Active project is not found");
            
            var tmpFileName = Path.GetTempFileName();
            File.AppendAllText(tmpFileName,
                "Current project: " + activeProject.FilePath + Environment.NewLine +
                "Tools of the project:" + Environment.NewLine
            );
            
            // tools list
            toolsList = activeProject.ToolsList;
            for (var i = 0; i < toolsList.Count; i++)
            {
                toolInfo = toolsList.ToolInfo[i];
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

                operations = toolsList.GetOperationsUsingTheTool(toolInfo.ToolID);
                operations.Reset();
                if (!operations.CurrentOperationIsEmpty())
                {
                    File.AppendAllText(tmpFileName,
                        "        Operations using the tool: " + Environment.NewLine);
                    while (!operations.CurrentOperationIsEmpty())
                    {
                        File.AppendAllText(tmpFileName,
                            "               " + operations.GetCurrentOperationCaption() + Environment.NewLine);
                        operations.MoveNext();
                    }
                }
            }

            // show
            Process.Start("notepad.exe", tmpFileName);
        } catch (Exception e) {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        } finally {
            // mandatory release of COM objects
            if (operations != null)
                Marshal.ReleaseComObject(operations);
            if (toolInfo != null)
                Marshal.ReleaseComObject(toolInfo);
            if (toolsList != null)
                Marshal.ReleaseComObject(toolsList);
            if (activeProject != null)
                Marshal.ReleaseComObject(activeProject);
        }
    }
}
