using System.Diagnostics;
using System.Runtime.InteropServices;
using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.Machine;
using CAMAPI.Project;
using CAMAPI.ResultStatus;

namespace ExtensionUtilityProjectMachineInfoNet;

/// <summary>
/// Utility to import geometry from Milling_25D\Part1.igs into the active project
/// </summary>
public class ExtensionProjectMachineInfo: IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        ICamApiProject? activeProject = null;
        ICamApiMachineInfo? machineInfo = null;
        try
        {
            // active project
            activeProject = context.CamApplication.GetActiveProject(out resultStatus);
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Can't get active project: " + resultStatus.Description);
            if (activeProject == null)
                throw new Exception("Active project is not found");
            
            // machine information
            machineInfo = activeProject.MachineInformation;
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
        } finally {
            // mandatory release of COM objects
            if (machineInfo != null)
                Marshal.ReleaseComObject(machineInfo);
            if (activeProject != null)
                Marshal.ReleaseComObject(activeProject);
        }
    }
}
