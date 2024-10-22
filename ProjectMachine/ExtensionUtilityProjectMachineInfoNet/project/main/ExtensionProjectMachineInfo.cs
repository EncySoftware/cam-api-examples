using System.Diagnostics;
using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.Machine;
using CAMAPI.Project;
using CAMAPI.ResultStatus;

namespace ExtensionUtilityProjectMachineInfoNet;

/// <summary>
/// Utility to get information about machine from the active project
/// </summary>
public class ExtensionProjectMachineInfo: IExtension, IExtensionUtility
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
            
            // machine information
            using var machineInfoCom = new ApiComObject<ICamApiMachineInfo>(activeProject.MachineInformation);
            var machineInfo = machineInfoCom.Instance;
            
            var tmpFileName = Path.GetTempFileName();
            File.WriteAllText(tmpFileName,
                "Current project: " + activeProject.FilePath + Environment.NewLine +
                "Machine caption: " + machineInfo.MachineCaption + Environment.NewLine +
                "Machine GUID: " + machineInfo.GUID + Environment.NewLine +
                "Machine file path: " + machineInfo.SchemaFilePath + Environment.NewLine +
                "Machine xml node name: " + machineInfo.XMLNodeName + Environment.NewLine
            );
            Process.Start("notepad.exe", tmpFileName);
        } catch (Exception e) {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
