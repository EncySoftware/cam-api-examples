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

    private static OperationTypeInfo? SelectOperation(List<OperationTypeInfo> items)
    {
        OperationTypeInfo? result = null;

        var thread = new Thread(() =>
        {
            var window = new TextInputWindow(items);

            if (window.ShowDialog() == true)
                result = window.SelectedItem;
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

            // List for operation types
            var operationTypeInfos = new List<OperationTypeInfo>();
            // get the list of operations
            var enumOperationTypes = technologistCom.EnumerateOperationTypes();
            using (var enumerator = enumOperationTypes.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var currentOperationType = enumerator.Current;
                    var currentOpTypeInstance = currentOperationType.Instance;

                    var operationTypeId = currentOpTypeInstance.Id;
                    var operationTypeCaption = currentOpTypeInstance.Caption;
                    var operationTypeDeclarationXmlFilePath = currentOpTypeInstance.DeclarationXmlFilePath;
                    operationTypeInfos.Add(
                        new OperationTypeInfo(operationTypeId, operationTypeCaption, operationTypeDeclarationXmlFilePath));
                }
            }

            // get user`s choice
            OperationTypeInfo selectedOperation = SelectOperation(operationTypeInfos);
            // if not selected
            if (selectedOperation == null)
                return;

            string selectedId = selectedOperation.Id;
            
            // create operation
            using var operationCom = technologistCom.InvokeAndWrap(technologist =>
                technologist.CreateOperation(selectedId, "", "", out var resultStatus));
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(resultStatus.Description);
            var id = operationCom.Invoke(operation => operation.Id);


            var operationType = operationCom.Invoke(operation => operation.OperationType);
            // Changing XML - props for TSTWaterlineOp 
            if (operationType == "TSTWaterlineOp")
            {
                using var xmlPropsCom = operationCom.InvokeAndWrap(operation => operation.XMLProp);
                xmlPropsCom.Invoke(xmlProps =>
                {
                    xmlProps.Bol["Roughing"] = true;
                    xmlProps.Str["Strategy"] = "Equidistant";
                    xmlProps.Int["StepCount"] = 8;
                    xmlProps.Str["UniteType"] = "ByLevel";
                    xmlProps.Flt["RoughingStep"] = 10.0;
                });
            }
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}