using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;
using CAMAPI.Project;

using System.Reflection.Emit;
using System.Runtime.InteropServices;
using CAMAPI.Logger;
using CAMAPI.UIDialogs.DotnetHelper;

namespace ExtensionOperationToolNet;

/// <summary>
/// Extension to create operation in the current project
/// </summary>
internal class ExtensionShowToolList : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    private static void ShowToolsList(List<string> items)
    {
        var thread = new Thread(() =>
        {
            var window = new TextInputWindow(items);
            window.ShowDialog();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
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
            using var activeProjectCom = applicationCom.InvokeAndWrap(application =>
                application.GetActiveProject(out var resultStatus));
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(resultStatus.Description);

            // get tool list
            using var toolListCom = activeProjectCom.InvokeAndWrap(project => project.ToolsList);
            var toolsCaptions = new List<string>();
            toolListCom.Invoke(toolList =>
            {
                for (var i = 0; i < toolList.Count; i++)
                {
                    using var toolCom = ComWrapper.Create(toolList.ToolInfo[i]);
                    var caption = toolCom.Invoke(tool => tool.ToolCaption);
                    toolsCaptions.Add(caption);
                }
            });

            //show tool list
            ShowToolsList(toolsCaptions);
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}