using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

namespace CreateUserOperationNet;

/// <summary>
/// Extension to create a user operation in the current project
/// </summary>
public class ExtensionCreateUserOperation : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Extension to create a user operation in the current project
    /// </summary>
    /// <param name="context">Information about the current running instance</param>
    /// <param name="resultStatus">Structure to return error</param>
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        
        try
        {
            // create a user operation
            using var applicationCom = ComWrapper.Create(context.CamApplication);
            using var projectCom = applicationCom.GetActiveProject();
            using var technologistCom = projectCom.Technologist();
            using var userOperationsCom = applicationCom.UserTechOperationList();
            using var currentOperationCom = technologistCom.CurrentOperation();
            var currentOperationId = currentOperationCom.Id();
            using var infoCom = userOperationsCom.AddFromOp(currentOperationCom.Name() + " NEW", currentOperationCom);
            var userOperationGuid = infoCom.GUID();
            
            // add to technology
            technologistCom.Invoke(technologist =>
            {
                technologist.CreateOperationFromUserTemplate(userOperationGuid, currentOperationId, out var ret);
                if (ret.Code == TResultStatusCode.rsError)
                    throw new Exception(ret.Description);
            });
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}