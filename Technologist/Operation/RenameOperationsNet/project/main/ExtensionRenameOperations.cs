using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;

namespace RenameOperationsNet;

/// <summary>
/// Extension to rename operations in the current project
/// </summary>
public class ExtensionRenameOperations : IExtension, IExtensionUtility
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
        
        try
        {
            using var applicationCom = ComWrapper.Create(context.CamApplication);
            
            // catch an active project
            using var projectCom = applicationCom.InvokeAndWrap(application => 
                (application.GetActiveProject(out var status), status));
            
            // get operations iterator
            using var technologistCom = projectCom.InvokeAndWrap(project => project.Technologist);
            using var operationCom = technologistCom.InvokeAndWrap(technologist =>
                (technologist.GetOperations(TCamApiReorderingMode.rmReordered, out var status), status));
            
            // do not rename machine - go deeper
            var exists = operationCom.Invoke(operation =>
            {
                operation.Reset();
                return operation.MoveToChild();
            });
            if (!exists)
            {
                resultStatus.Code = TResultStatusCode.rsError;
                resultStatus.Description = "No operations found in the current project.";
                return;
            }
            
            // iterate through operations and rename them
            RenameOperations(operationCom, string.Empty, 0);
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
    
    private void RenameOperations(ComWrapper<ICamApiTechOperationIterator> operationIterator, string prefix, int counter)
    {
        counter++;
        
        // rename current operation
        using var operationCom = operationIterator.InvokeAndWrap(iterator => iterator.Current());
        var localPrefix = prefix;
        operationCom.Invoke(operation =>
        {
            operation.Name = $"{localPrefix}{counter} {operation.Name}";
        });
        
        // go to child operation
        if (operationIterator.Invoke(iterator => iterator.MoveToChild()))
        {
            // rename child operations
            RenameOperations(operationIterator, $"{prefix}{counter}.", 0);
            
            // go back to the parent operation
            operationIterator.Invoke(iterator => iterator.MoveToParent());
        }
        
        // go to the next sibling operation
        if (operationIterator.Invoke(iterator => iterator.MoveToSibling()))
            RenameOperations(operationIterator, prefix, counter);
    }
}