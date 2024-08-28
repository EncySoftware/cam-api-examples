using System.Diagnostics;
using System.Runtime.InteropServices;
using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

namespace ExtensionNetProject;

/// <summary>
/// Extension to demonstrate entry point "utility" 
/// </summary>
public class ExtensionUtility : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Utility to create copy of current project in another folder
    /// </summary>
    /// <param name="context">Information about current running instance</param>
    /// <param name="resultStatus">Structure to return error</param>
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            // get context
            var application = context.CamApplication;
            var currentFolder = Path.GetDirectoryName(context.Constants.InstallFolder);
            if (currentFolder == null)
                throw new Exception("Cannot get current folder");

            // export
            var exportedFile = Path.Combine(currentFolder, "exported.stcp");
            application.ExportCurrentProject(exportedFile, true, out resultStatus);
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Error exporting project: " + resultStatus.Description);
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}