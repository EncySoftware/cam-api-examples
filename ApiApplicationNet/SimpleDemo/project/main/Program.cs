using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.ResultStatus;
using CAMHelper.NativeLibUtils;
using STLoggingInterface;

namespace ApplicationSimpleDemoNet;

internal static class Program
{
    private static int Main(string[] args)
    {
        IST_Logger? logger = null;
        try
        {
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
            StarterNativeUtils.FreeLibrary();
            PathAliases.FreeLibrary();
        }

        return 0;
    }
}
