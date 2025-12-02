using System;
using System.Runtime.InteropServices;
using CAMAPI.DotnetHelper;
using CAMAPI.ResultStatus;
using CAMAPI.Singletons;
using STLoggingInterface;

namespace ApplicationFullWorkflow3DProjectNet;

internal static class Program
{
    private static int Main(string[] args)
    {
        IST_Logger? logger = null;
        try
        {
            RunInternal(logger, args);    
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error: " + ex.Message);
        }
        finally
        {
            KernelConsole.FreeLibrary();
            StarterNativeUtils.FreeLibrary();
            PathAliases.FreeLibrary();
        }

        return 0;
    }

    private static void RunInternal(IST_Logger? logger, string[] args){
        var list = PathAliases.InitFolders("SCConsole.exe", includePrIdInPath: false);
        
        if (list != null)
        {
            var companyName = list.CompanyName;
            if (!string.IsNullOrEmpty(companyName))
                Console.WriteLine($"CompanyName: {companyName}");
        }


        logger = StarterNativeUtils.InitLogs(aprocDependentFileName: false) 
            ?? throw new Exception("Logger initialization failed.");
    

        string @params = string.Join(' ', args);
        using var applicationCom = ComWrapper.Create(KernelConsole.Run(@params));


        var executablePath = applicationCom.It.ExecutablePath;
        if (!string.IsNullOrEmpty(executablePath))
            Console.WriteLine($"Executable path: {executablePath}");
        

        var extensionManager = applicationCom.It.GetExtensionManager(out TResultStatus resultStatus) 
                    ?? throw new Exception("GetExtensionManager failed.");
        
        var apiVersion = extensionManager.ApiVersion;
        if (!string.IsNullOrEmpty(apiVersion))
            Console.WriteLine($"Extension Manager API Version: {apiVersion}");


        using var pathsHelperCom = SystemExtensionFactory.GetSingletonExtension<ICamApiPaths>("Extension.Global.Singletons.Paths")
                                   ?? throw new Exception("PathsHelper is failed");

        using var activeProjectCom = applicationCom.InvokeAndWrap(application =>
            (application.GetActiveProject(out var status), status)) 
                                     ?? throw new Exception("Active project is not found");
        
        using var geometryModelCom = activeProjectCom.InvokeAndWrap(project => project.CAMAPIGeomModel)
                                     ?? throw new Exception("Geometry model is not found");
        using var technologistCom = activeProjectCom.InvokeAndWrap(project => project.Technologist)
                                     ?? throw new Exception("Technologist is not found");
        
        
        try{
            //prepare model
            ModelHelper.PrepareModel(applicationCom, activeProjectCom, pathsHelperCom);
            
            // prepare the project
            ProjectHelper.PrepareProject(applicationCom, pathsHelperCom);
            
            // create technology
            TechnologyHelper.CreateTechnology(applicationCom,
                activeProjectCom,
                technologistCom,
                geometryModelCom,
                pathsHelperCom);
            
            // // simulate and save results
            SimulationHelper.RunSimulation(applicationCom, activeProjectCom);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error: " + ex.Message);
        }
        finally
        {
            Marshal.ReleaseComObject(extensionManager);
            if (logger != null)
            {
                Marshal.ReleaseComObject(logger);
                logger = null;
            }
        }
        
    }
}
