// using CAMAPI.Application;
// using CAMAPI.DotnetHelper;
// using CAMAPI.Extensions;
// using CAMAPI.ResultStatus;
// using CAMAPI.TechOperation;
// using CAMAPI.Project;

// using System.Reflection.Emit;
// using System.Runtime.InteropServices;
// using CAMAPI.Logger;
// using CAMAPI.UIDialogs.DotnetHelper;

// namespace ExtensionOperationToolNet;

// /// <summary>
// /// Extension to create operation in the current project
// /// </summary>
// internal class ExtensionToolChange : IExtension, IExtensionUtility
// {
//     /// <inheritdoc />
//     public IExtensionInfo? Info { get; set; }

//     /// <summary>
//     /// Rename operations in the current project
//     /// </summary>
//     /// <param name="context">Information about current running instance</param>
//     /// <param name="resultStatus">Structure to return error</param>
//     public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
//     {
//         resultStatus = default;
//         // We use ComWrapper everywhere to limit the lifetime of the COM objects and 
//         // make it predicable for CAM system and not rely on the garbage collector.
//         try
//         {
//             using var applicationCom = ComWrapper.Create(context.CamApplication);

//             // catch an active project
//             using var activeProjectCom = applicationCom.InvokeAndWrap(application =>
//                 application.GetActiveProject(out var resultStatus));
//             if (resultStatus.Code == TResultStatusCode.rsError)
//                 throw new Exception(resultStatus.Description);

//             // get tool list
//             using var toolListCom = activeProjectCom.InvokeAndWrap(project => project.ToolsList);
//             var toolsCaptions = new List<string>();
//             toolListCom.Invoke(toolList =>
//             {
//                 for (var i = 0; i < toolList.Count; i++)
//                 {
//                     using var toolCom = ComWrapper.Create(toolList.ToolInfo[i]);
//                     var caption = toolCom.Invoke(tool => tool.ToolCaption);
//                     toolsCaptions.Add(caption);
//                 }
//             });

//             ////// change tool of current operation to the last tool in the list
//             // get technologist
//             using var technologistCom = activeProjectCom.InvokeAndWrap(project => project.Technologist);

//             // get current operation
//             using var currentOperationCom = technologistCom.InvokeAndWrap(technologist => technologist.CurrentOperation);

//             var operationId = currentOperationCom.Invoke(op => op.Id);
            
//             var lastOperationToolId = toolListCom.Invoke(toolList =>
//             {
//                 var lastToolIndex = toolList.Count - 1;
//                 if (lastToolIndex < 0)
//                     return string.Empty;
//                 using var toolCom = ComWrapper.Create(toolList.ToolInfo[lastToolIndex]);
//                 return toolCom.Invoke(tool => tool.ToolID);
//             });
            
//             activeProjectCom.Invoke(project =>
//             {
//                 project.SetOperationTool(operationId, lastOperationToolId, out var executeContext);
//                 if (executeContext.Code == TResultStatusCode.rsError)
//                     throw new Exception(executeContext.Description);
//             });
//         }
//         catch (Exception e)
//         {
//             resultStatus.Code = TResultStatusCode.rsError;
//             resultStatus.Description = e.Message;
//         }
//     }
// }