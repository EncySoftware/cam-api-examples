using System;
using System.IO;
using CAMAPI.Application;
using CAMAPI.ApplicationMainForm;
using CAMAPI.DotnetHelper;
using CAMAPI.ResultStatus;
using CAMIPC.Application;
using CAMIPC.ExecuteContext;
using CAMIPC.Singletons;

namespace FullWorkflow3DProject;

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
    public static void PrepareProject(ComWrapper<ICamIpcApplication> applicationCom,
        ComWrapper<ICamIpcPaths> pathsHelperCom)
    {
        // switch to model tab
        var executeContext = new TExecuteContext();
        applicationCom.Invoke(application =>
        {
            application.SetMainWorkMode(TMainWorkMode.mwmMachining, ref executeContext);
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
        });
        
        // change machine
        ChangeMachine(applicationCom, pathsHelperCom);
            
        // setup machining tools
        SetupMachiningTools(applicationCom, pathsHelperCom);
    }

    /// <summary>
    /// Change machine of the project
    /// </summary>
    private static void ChangeMachine(ComWrapper<ICamIpcApplication> applicationCom,
        ComWrapper<ICamIpcPaths> pathsHelperCom)
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
                "DMU60");
            if (machine == null)
                throw new Exception("Cannot find machine Leader");
            return machine;
        });

        // set the machine to the project
        var executeContext = new TExecuteContext();
        machineInfoCom.Invoke(machineInfo =>
        {
            applicationCom.Invoke(application =>
            {
                application.SetActiveProjectMachine(machineInfo, ref executeContext);
                if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                    throw new Exception(executeContext.ResultStatus.Description);
            });
        });

    }

    private static void SetupMachiningTools(ComWrapper<ICamIpcApplication> applicationCom,
        ComWrapper<ICamIpcPaths> pathsHelperCom)
    {
        // path to the tool library
        var libraryFolder = pathsHelperCom.Invoke(pathsHelper => pathsHelper.LibrariesFolder)
            ?? throw new Exception("Cannot get tool libraries folder");
        var toolsFolder = Path.Combine(libraryFolder, "Tools", "Examples");
        var toolsLibraryPath = Path.Combine(toolsFolder, "ToolKit.db");
        
        // add
        var executeContext = new TExecuteContext();
        using var machiningToolManagerCom =
            applicationCom.InvokeAndWrap(application => application.GetMachiningToolsManager(ref executeContext));
        if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        
        machiningToolManagerCom.Invoke(manager =>
        {
            manager.AddToolToProject(toolsLibraryPath, "11", ref executeContext);
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
            
            manager.AddToolToProject(toolsLibraryPath, "12", ref executeContext);
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
            
            manager.AddToolToProject(toolsLibraryPath, "35", ref executeContext);
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
            
            manager.AddToolToProject(toolsLibraryPath, "75", ref executeContext);
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
            
            manager.AddToolToProject(toolsLibraryPath, "63", ref executeContext);
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
        });
    }
}