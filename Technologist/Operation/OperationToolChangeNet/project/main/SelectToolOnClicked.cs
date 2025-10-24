using System.Diagnostics;
using CAMAPI.Application;
using CAMAPI.NCMaker;
using CAMAPI.ResultStatus;
using CAMAPI.Singletons;
using CAMAPI.TechnologyForm;
using CAMAPI.DotnetHelper;


namespace ExtensionOperationToolPopupNet;

/// <summary>
/// Make NC for selected operation
/// </summary>
public class SelectToolOnClicked : ICamApiTechnologyFormOperationPopupItemOnClicked
{
    /// <summary>
    /// Show tools list
    /// </summary>
    private static (string? libraryName, string? toolId, string? filePath) ShowToolsList(List<string> items)
    {
        string? libraryName = null;
        string? toolId = null;
        string? filePath = null;
        var thread = new Thread(() =>
        {
            var window = new TextInputWindow(items);
            if (window.ShowDialog() == true)
            {
                libraryName = window.LibraryName;
                toolId = window.ToolId;
                filePath = window.SelectedFilePath;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        return (libraryName, toolId, filePath);
    }
    /// <summary>
    /// Make NC for selected operation
    /// </summary>
    public void OnItemClicked(IExtensionOperationPopupItemOnClickedContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        
        try
        {   
            using var applicationSingletonCom =
                SystemExtensionFactory.GetSingletonExtension<ICamApiApplicationSingleton>("Extension.Global.Singletons.Application");
            var applicationSingleton = applicationSingletonCom.Instance
                                  ?? throw new NullReferenceException("Failed to create application singleton object");
            using var applicationCom = new ComWrapper<ICamApiApplication>(applicationSingleton.GetApplication(out _));

            using var projectCom = ComWrapper.Create(context.ActiveProject);

            using var toolListCom = projectCom.InvokeAndWrap(project => project.ToolsList);
            
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
            
            var (libraryName, toolId, filePath) = ShowToolsList(toolsCaptions);
            if (libraryName == null || toolId == null || filePath == null){
                throw new Exception("One of the tool`s params is null");
            }

            using var machiningToolManagerCom = applicationCom.InvokeAndWrap(application => application.MachiningToolsManager);
            machiningToolManagerCom.Invoke(manager =>
            {
                manager.AddToolToProject(filePath, toolId, out var ret);
                if (ret.Code == TResultStatusCode.rsError)
                    throw new Exception(ret.Description);
            });
            toolsCaptions.Clear();
            toolListCom.Invoke(toolList =>
            {
                for (var i = 0; i < toolList.Count; i++)
                {
                    using var toolCom = ComWrapper.Create(toolList.ToolInfo[i]);
                    var caption = toolCom.Invoke(tool => tool.ToolCaption);
                    toolsCaptions.Add(caption);
                }
            });
        }
        catch (Exception ex)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = ex.Message;
        }
    }
}