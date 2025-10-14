using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;

namespace ExtensionOperationsNet;

/// <summary>
/// Extension to create operation in the current project
/// </summary>
internal class ExtensionCreatePart : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Rename operations in the current project
    /// </summary>
    /// <param name="context">Information about current running instance</param>
    /// <param name="resultStatus">Structure to return error</param>
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        // We use ComWrapper everywhere to limit the lifetime of the COM objects and 
        // make it predicable for CAM system and not rely on the garbage collector.
        try
        {
            using var applicationCom = ComWrapper.Create(context.CamApplication);

            // catch an active project
            using var projectCom = applicationCom.InvokeAndWrap(application =>
                application.GetActiveProject(out var resultStatus));
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(resultStatus.Description);

            // get technologist
            using var technologistCom = projectCom.InvokeAndWrap(project => project.Technologist);

            // create part
            using var partCom = technologistCom.InvokeAndWrap(technologist =>
                technologist.CreatePart(100, out var status));
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(resultStatus.Description);
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}