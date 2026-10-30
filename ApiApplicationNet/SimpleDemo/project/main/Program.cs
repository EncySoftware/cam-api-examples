using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.ResultStatus;
using CAMHelper.NativeLibUtils;
using PathAliases;
using STLoggingInterface;

namespace ApplicationSimpleDemoNet;

internal static class Program
{
    private static int Main(string[] args)
    {
        IST_Logger? logger = null;
        try
        {
            var aliases = PathAliasesFacade.InitFolders();
            var companyName = aliases.CompanyName;
            if (!string.IsNullOrEmpty(companyName))
                Console.WriteLine($"CompanyName: {companyName}");


            logger = StarterNativeUtils.InitLogs(aprocDependentFileName: false) 
                ?? throw new Exception("Logger initialization failed.");
            StarterNativeUtils.SetAppStarter(new SCAppStarterShell());
        

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

            Marshal.ReleaseComObject(extensionManager);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Error: " + ex.Message);
        }
        finally
        {
            if (logger != null)
            {
                Marshal.ReleaseComObject(logger);
                logger = null;
            }

            KernelConsole.FreeLibrary();
            StarterNativeUtils.DetachAndStop();
            StarterNativeUtils.FreeLibrary();
        }

        return 0;
    }
}
