using System.Diagnostics;
using CAMAPI.ApplicationMainForm;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.Project;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;

namespace ExtensionUtilityProjectToolsListNet;

/// <summary>
/// Extension to demonstrate entry point "project_report_popup"
/// </summary>
public class ExtensionProjectReportPopup : IExtension, IExtensionProjectReportPopup
{
    private static readonly string[] ItemCaptions =
    [
        "Show project tools list",
        "Show project tools list1",
        "Show project tools list2"
    ];

    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public int ItemsCount => ItemCaptions.Length;

    /// <inheritdoc />
    public string GetItemCaption(int itemIndex) => ItemCaptions[itemIndex];

    /// <inheritdoc />
    public void ExecuteItem(int itemIndex, ICamApiProject activeProject, out TResultStatus resultStatus)
    {
        resultStatus = default;

        try
        {
            using var projectCom = ComWrapper.Create(activeProject);

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
        catch (Exception ex)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = ex.Message;
        }
    }

    private static void RenameOperations(ComWrapper<ICamApiTechOperationIterator> operationIterator, string prefix, int counter)
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