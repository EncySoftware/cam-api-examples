using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;

using System.Reflection.Emit;
using System.Runtime.InteropServices;
using CAMAPI.Logger;
using CAMAPI.UIDialogs.DotnetHelper;

namespace ExtensionOperationsNet;

/// <summary>
/// Extension to create operation in the current project
/// </summary>
internal class ExtensionCreateOperation : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    private static string? SelectOperation(List<string> items)
    {
        string? result = null;

        var thread = new Thread(() =>
        {
            var window = new TextInputWindow(items);

            if (window.ShowDialog() == true)
                result = window.UserInput;
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        return result;
    }
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

            // get the list of operations
            var ids = new List<string>();
            technologistCom.Invoke(technologist =>
            {
                var operationTypes = technologist.GetAvailableOperationTypeIds(out var executeContext);
                if (executeContext.Code == TResultStatusCode.rsError)
                    throw new Exception(executeContext.Description);

                for (var i = 0; i < operationTypes.Count(); i++)
                    ids.Add(operationTypes.Get(i));
            });

            // get user`s choice
            string selectedId = SelectOperation(ids);
            if (selectedId == null)
                return;

            // create operation
            using var operationCom = technologistCom.InvokeAndWrap(technologist =>
                technologist.CreateOperation(selectedId, "", "", out var resultStatus));
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(resultStatus.Description);
            var id = operationCom.Invoke(operation => operation.Id);

            using var xmlPropsCom = operationCom.InvokeAndWrap(operation => operation.XMLProp);
            xmlPropsCom.Invoke(xmlProps =>
            {
                xmlProps.Bol["Roughing"] = true; //for TSTWaterlineOp
            });
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}