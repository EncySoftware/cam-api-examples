using CAMAPI.Application;
using CAMAPI.ResultStatus;
using CAMAPI.Singletons;
using CAMAPI.TechnologyForm;
using CAMAPI.DotnetHelper;
using System.IO;

namespace ExtensionOperationToolPopupNet;

/// <summary>
/// Make NC for selected operation
/// </summary>
public class SelectToolOnClicked : ICamApiTechnologyFormOperationPopupItemOnClicked
{
    /// <summary>
    /// Show tools list
    /// </summary>
    private TextInputWindow? currentWindow;
    /// <summary>
    /// Action to show error if tool not found
    /// </summary>
    public event Action<string, string>? OnToolNotFound;
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
            
            using var extensionManagerCom = ExtensionManagerHelper.GetInstance();
            
            var projectCom = ComWrapper.Create(context.ActiveProject);

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
            
            string? libraryName = null;
            string? toolId = null;
            
            var thread = new Thread(() =>
            {
                currentWindow = new TextInputWindow(toolsCaptions);
                OnToolNotFound += currentWindow.HandleToolNotFound;
                currentWindow.OnToolAdded += (libName, tId) =>
                {
                    libraryName = libName;
                    toolId = tId;
                    
                    // get filepath from all user`s documents folder
                    using var pathsHelperCom = SystemExtensionFactory.GetSingletonExtension<ICamApiPaths>("Extension.Global.Singletons.Paths");
                    var filePath = pathsHelperCom.Invoke(pathsHelper =>
                    {
                        var resultFilePath = Path.Combine(pathsHelper.LibrariesFolder, "Tools", "Examples", libraryName);
                        if (!File.Exists(resultFilePath))
                            throw new Exception("Library not found: " + resultFilePath);
                        return resultFilePath;
                    });


                    if (libraryName != null && toolId != null)
                    {
                        using var applicationCom = applicationSingletonCom.InvokeAndWrap(applicationSingleton => 
                            applicationSingleton.GetApplication(out var rs)); 
                        using var machiningToolManagerCom = applicationCom.InvokeAndWrap(application => application.MachiningToolsManager);
                        
                        machiningToolManagerCom.Invoke(manager =>
                        {
                            manager.AddToolToProject(filePath, toolId, out var ret);
                            if (ret.Code == TResultStatusCode.rsError){
                                OnToolNotFound?.Invoke(toolId, filePath);
                                return;
                            }   
                        });
                        
                        using var projectInThreadCom = applicationCom.InvokeAndWrap(application => application.GetActiveProject(out var rs));
                        using var toolListInThreadCom = projectInThreadCom.InvokeAndWrap(project => project.ToolsList);
                        
                        toolsCaptions.Clear();
                        toolListInThreadCom.Invoke(toolList =>
                        {
                            for (var i = 0; i < toolList.Count; i++)
                            {
                                using var toolCom = ComWrapper.Create(toolList.ToolInfo[i]);
                                var caption = toolCom.Invoke(tool => tool.ToolCaption);
                                toolsCaptions.Add(caption);
                            }
                        });
                        
                        currentWindow.UpdateItems(toolsCaptions);
                        libraryName = null;
                        toolId = null;
                        filePath = null;
                    }
                };
                currentWindow.Closed += (_, __) => OnToolNotFound -= currentWindow.HandleToolNotFound;
                currentWindow.ShowDialog();
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
        catch (Exception ex)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = ex.Message;
        }
    }
}