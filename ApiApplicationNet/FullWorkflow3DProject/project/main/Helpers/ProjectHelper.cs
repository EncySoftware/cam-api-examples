using System;
using System.IO;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.ResultStatus;
using CAMAPI.Singletons;

namespace ApplicationFullWorkflow3DProjectNet;

/// <summary>
/// Operations over project settings
/// </summary>
public class ProjectHelper
{
    /// <summary>
    /// Relative path to the machine file
    /// </summary>
    private const string MachineFilePath = @"Schemas\Milling\4-axis\Lider\Lider.xml";

    /// <summary>
    /// Prepare the project: change machine, setup machining tools
    /// </summary>
    public static void PrepareProject(ComWrapper<ICamApiApplication> applicationCom,
        ComWrapper<ICamApiPaths> pathsHelperCom)
    {
        // switch to model tab
        applicationCom.Invoke(application =>
        {
            application.MainWorkMode = TMainWorkMode.mwmMachining;
        });
        
        // change machine
        ChangeMachine(applicationCom, pathsHelperCom);
            
        // setup machining tools
        SetupMachiningTools(applicationCom, pathsHelperCom);
    }

    /// <summary>
    /// Change machine of the project
    /// </summary>
    private static void ChangeMachine(ComWrapper<ICamApiApplication> applicationCom,
        ComWrapper<ICamApiPaths> pathsHelperCom)
    {
        // get the path to the machine file
        var machinesFolder = pathsHelperCom.Invoke(pathsHelper => pathsHelper.MachinesFolder)
            ?? throw new Exception("Cannot get machines folder");
        var machineFile = Path.Combine(machinesFolder, MachineFilePath);
        if (!File.Exists(machineFile))
            throw new Exception($"Cannot find machine file: {machineFile}");
        
        // find the machine in the library
        using var machinesLibraryCom = applicationCom.InvokeAndWrap(application => application.MachinesLibrary);
        using var machineInfoCom = machinesLibraryCom.InvokeAndWrap(machinesLibrary =>
        {
            var machine = machinesLibrary.FindMachine(
                Guid.Parse("F4BD0FFD-8C44-4CFC-A865-CF595EEEF38F"),
                machineFile,
                "DMU60") 
                          ?? throw new Exception("Cannot find machine Leader");
            return machine;
        });
        var machineInfo = machineInfoCom.It
                          ?? throw new Exception("Cannot get machine info");

        // set the machine to the project
        applicationCom.Invoke(application =>
        {
            application.SetActiveProjectMachine(machineInfo, out var ret);
            if (ret.Code == TResultStatusCode.rsError)
                throw new Exception(ret.Description);
        });
    }

    private static void SetupMachiningTools(ComWrapper<ICamApiApplication> applicationCom,
        ComWrapper<ICamApiPaths> pathsHelperCom)
    {
        // path to the tool library
        var libraryFolder = pathsHelperCom.Invoke(pathsHelper => pathsHelper.LibrariesFolder)
            ?? throw new Exception("Cannot get tool libraries folder");
        var toolsFolder = Path.Combine(libraryFolder, "Tools", "Examples");
        var toolsLibraryPath = Path.Combine(toolsFolder, "ToolKit.db");
        
        // add
        using var machiningToolManagerCom = applicationCom.InvokeAndWrap(application => application.MachiningToolsManager);
        machiningToolManagerCom.Invoke(manager =>
        {
            manager.AddToolToProject(toolsLibraryPath, "11", out var ret);
            if (ret.Code == TResultStatusCode.rsError)
                throw new Exception(ret.Description);
            
            manager.AddToolToProject(toolsLibraryPath, "12", out ret);
            if (ret.Code == TResultStatusCode.rsError)
                throw new Exception(ret.Description);
            
            manager.AddToolToProject(toolsLibraryPath, "35", out ret);
            if (ret.Code == TResultStatusCode.rsError)
                throw new Exception(ret.Description);
            
            manager.AddToolToProject(toolsLibraryPath, "75", out ret);
            if (ret.Code == TResultStatusCode.rsError)
                throw new Exception(ret.Description);
            
            manager.AddToolToProject(toolsLibraryPath, "63", out ret);
            if (ret.Code == TResultStatusCode.rsError)
                throw new Exception(ret.Description);
        });
    }
}