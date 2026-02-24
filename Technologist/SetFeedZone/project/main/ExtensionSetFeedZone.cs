using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

namespace SetFeedZone;

/// <summary>
/// Utility to manage the main window view
/// </summary>
public class ExtensionSetFeedZone: ExtensionWindowLazyUnloadable, IExtension, IExtensionUtility
{
    
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }
    
    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    { 
        resultStatus = default;
        try
        {
            // arrange
            ComWrapperSettings.ApplicationApartmentState = ApartmentState.STA;
            using var applicationCom = ComWrapper.Create(context.CamApplication);
            SetOwnerHandle(applicationCom);
            using var projectCom = applicationCom.GetActiveProject();
            using var technologistCom = projectCom.Technologist();
            using var operationCom = technologistCom.CurrentOperation();
            var jobAssignmentCom = operationCom.ModelFormerJobAssignment();
            
            // show form
            ShowWindow(() => new ViewControlWindow(jobAssignmentCom));
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
