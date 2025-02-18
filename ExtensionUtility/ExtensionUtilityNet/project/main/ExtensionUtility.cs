using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.Singletons;
using CAMAPI.DotnetHelper;

namespace ExtensionUtilityNet;

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
            // get global context
            using var pathsCom = SystemExtensionFactory.GetSingletonExtension<ICamApiPaths>("Extension.Global.Singletons.Paths");
            var paths = pathsCom.Instance
                ?? throw new Exception("Paths singleton not found");
            
            // export
            using var applicationCom = new ComWrapper<ICamApiApplication>(context.CamApplication);
            var application = applicationCom.Instance
                ?? throw new Exception("Application not found");
            
            var exportedFile = Path.Combine(paths.MainProgramFolder, "exported.stcp");
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