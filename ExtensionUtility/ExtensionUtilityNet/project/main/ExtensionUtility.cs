using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.DotnetHelper;
using CAMAPI.UIDialogs.DotnetHelper;

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
            // select output file
            using var dialogsHelperCom = UIDialogs.CreateHelper();
            var exportedFolder = dialogsHelperCom.Invoke(helper =>
            {
                var result = string.Empty;
                return helper.SelectFolderDialog("Select folder to export project", ref result)
                    ? result
                    : string.Empty;
            });
            if (string.IsNullOrEmpty(exportedFolder))
                return;
            
            // export
            var exportedFile = Path.Combine(exportedFolder, "exported.stcp");
            using var applicationCom = ComWrapper.Create(context.CamApplication);
            var application = applicationCom.Instance
                              ?? throw new Exception("Application not found");
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