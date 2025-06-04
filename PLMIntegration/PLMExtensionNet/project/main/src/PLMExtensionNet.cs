using PLMIntegrarionExamples.Items;
using PLMIntegrarionExamples.Parameters;
using PLMIntegrarionExamples.Common;
using PLMIntegrarionExamples.Tree;
using CAMAPI.Extensions;
using CAMAPI.Extension.PLM;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;
using System.Reflection;

namespace PLMIntegrarionExamples;

class PLMExtensionNet : IExtensionPLM, IExtension
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Gets a value indicating whether this PLM extension supports working with projects.
    /// </summary>
    public bool SupportProjectLoad { get => true; }

    /// <summary>
    /// Gets a value indicating whether this PLM extension supports working with machines.
    /// </summary>
    public bool SupportMachineLoad { get => true; }

    /// <summary>
    /// Gets a value indicating whether this PLM extension supports working with postproccesors.
    /// </summary>
    public bool SupportPostprocessorLoad { get => true; }

    /// <summary>
    /// Gets a value indicating whether this PLM extension supports working with postproccesors inside machines.
    /// </summary>
    public bool SupportPostprocessorInsideMachineLoad { get => false; }

    /// <summary>
    /// Gets a value indicating whether this PLM extension supports loading tools.
    /// </summary>
    public bool SupportToolLoad { get => true; }

    /// <summary>
    /// Gets a value indicating whether this PLM extension supports savings tools.
    /// </summary>
    public bool SupportToolSave { get => true; }

    /// <summary>
    /// Gets a value indicating whether this PLM extension supports the domain authentification.
    /// </summary>    
    public bool SupportDomainAuth { get => true; }

    private PLMParameters plmParams;
    
    private PLMDirectoryHelper? machinesDirectoryHelper;

    private PLMDirectoryHelper? postprocessorsDirectoryHelper;

    private PLMDirectoryHelper? modelsDirectoryHelper;

    private PLMDirectoryHelper? projectsDirectoryHelper;

    private PLMDirectoryHelper? toolsDirectoryHelper;

    private const string emptyElementId = "EmptyElementId";

    private readonly string defaultDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PLMExtensionNet"/> class.
    /// </summary>
    public PLMExtensionNet()
    {
        defaultDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\PLMData";
        plmParams = new PLMParameters(defaultDirectory);
    }

    /// <summary>
    /// Sets the language and code page for the PLM extension.
    /// </summary>
    /// <param name="languageID">The identifier of the language to be set.</param>
    /// <param name="codePage">The code page associated with the selected language.</param>
    public void SetLanguage(uint languageID, byte codePage)
    {
        // setting language
    }

    /// <summary>
    /// Retrieves the parameters of the PLM extension.
    /// </summary>
    /// <returns>An <see cref="IPLMParameters"/> instance containing extension parameters.</returns>
    public IPLMParameters GetParameters() => plmParams;

    /// <summary>
    /// Establishes a connection to the PLM extension using the provided parameters.
    /// </summary>
    /// <param name="values">The parameter values required for the connection.</param>
    /// <param name="connectionId">A unique identifier for the connection session.</param>
    /// <param name="useDomainAuth">
    /// A boolean value indicating whether domain authentication should be used.
    /// </param>
    /// <returns>An <see cref="IPLMResult"/> instance indicating the result of the connection.</returns>
    public IPLMResult Connect(IPLMParameterValues values, Guid connectionId, bool useDomainAuth)
    {
        plmParams.SetParameterValues(values);

        var plmFolderPath = plmParams["PLMFolder"];

        try
        {
            if (plmFolderPath == defaultDirectory)
            {
                using var dialogsHelperCom = UIDialogs.CreateHelper();
                var helper = dialogsHelperCom.Instance
                    ?? throw new Exception("Failed to create UIDialogs helper");
                // var buttons = MessageBoxHelper.BuildButtons(TUIButtonType.btYes, TUIButtonType.btNo);
                // var useDefaultPath = dialogsHelperCom.Invoke(helper =>
                // {
                //     return helper.MessageBox($"Parameter with the path to the PLM folder was not set.\nDo you want to use the default path:\n\"{defaultDirectory}\"?",
                //         TMessageDialogType.mdtInformation, buttons, TUIButtonType.btYes, "");
                // });

                // if (useDefaultPath == TUIButtonType.btNo)
                // {
                //     return new PLMResult {
                //         Code = 1,
                //         ErrorMessage = "Please set the PLM Folder parameter in the Settings"
                //     };
                // }
                var buttons = MessageBoxHelper.BuildButtons(TUIButtonType.btOk);
                dialogsHelperCom.Invoke(helper =>
                {
                    return helper.MessageBox($"Parameter with the path to the PLM folder was not set.\nDefault path will be used:\n\"{defaultDirectory}\".",
                        TMessageDialogType.mdtInformation, buttons, TUIButtonType.btOk, "");
                });
            }

            var machDirName = Path.Combine(plmFolderPath, "Machines");
            machinesDirectoryHelper = new PLMDirectoryHelper(machDirName);
            var ppDirName = Path.Combine(plmFolderPath, "Postprocessors");
            postprocessorsDirectoryHelper = new PLMDirectoryHelper(ppDirName);
            var modDirName = Path.Combine(plmFolderPath, "Models");
            modelsDirectoryHelper = new PLMDirectoryHelper(modDirName);
            var projDirName = Path.Combine(plmFolderPath, "Projects");
            projectsDirectoryHelper = new PLMDirectoryHelper(projDirName);
            var toolsDirName = Path.Combine(plmFolderPath, "Tools");
            toolsDirectoryHelper = new PLMDirectoryHelper(toolsDirName);
        }
        catch (Exception ex)
        {
            return new PLMResult
            {
                Code = 1,
                ErrorMessage = $"An exception occured while connecting to PLM. Exception message: {ex.Message}"
            };
        }

        return ReturnSuccessfulResult();
    }

    /// <summary>
    /// Disconnects from the PLM extension.
    /// </summary>
    /// <returns>An <see cref="IPLMResult"/> instance indicating the result of the disconnection.</returns>
    public IPLMResult Disconnect() => ReturnSuccessfulResult();

    /// <summary>
    /// Performs the necessary setup steps for integrating the PLM extension.  
    /// with the environment.
    /// </summary>
    /// <returns>An <see cref="IPLMResult"/> instance indicating the success or failure of the installation.</returns>
    public IPLMResult Install() => ReturnSuccessfulResult();

    /// <summary>
    /// Performs any necessary cleanup to ensure a proper uninstallation.
    /// </summary>
    /// <returns>An <see cref="IPLMResult"/> instance indicating the result of the uninstallation.</returns>
    public IPLMResult Uninstall() => ReturnSuccessfulResult();

    /// <summary>
    /// Retrieves an item from the PLM extension based on its type and identifier.
    /// </summary>
    /// <param name="itemType">The type of the item.</param>
    /// <param name="itemId">The unique identifier of the item.</param>
    /// <param name="items">The output parameter containing the retrieved item.</param>
    /// <returns>An <see cref="IPLMResult"/> instance indicating the result of the operation.</returns>
    public IPLMResult GetItem(TPLMItemType itemType, string itemId, out IPLMTree? items)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            items = null;
            return new PLMResult {
                Code = 1,
                ErrorMessage = "The item identificator is empty"
            };
        }
        
        string[] directories;
        if (itemId.Contains(emptyElementId))
            directories = [];
        else switch (itemType)
            {
                case TPLMItemType.itMachine:
                    directories = [machinesDirectoryHelper?.FindSubdirectoryByExactName(itemId) ?? string.Empty];
                    break;
                case TPLMItemType.itPostprocessor:
                    directories = [postprocessorsDirectoryHelper?.FindSubdirectoryByExactName(itemId) ?? string.Empty];
                    break;
                case TPLMItemType.itModel:
                case TPLMItemType.itWorkpiece:
                    directories = [modelsDirectoryHelper?.FindSubdirectoryByExactName(itemId) ?? string.Empty];
                    break;
                case TPLMItemType.itProject:
                    directories = [projectsDirectoryHelper?.FindSubdirectoryByExactName(itemId) ?? string.Empty];
                    break;
                case TPLMItemType.itTool:
                    directories = [toolsDirectoryHelper?.FindSubdirectoryByExactName(itemId) ?? string.Empty];
                    break;
                default:
                    items = new PLMTree();
                    return ReturnSuccessfulResult();
            }
        
        items = GetPLMTree(itemType, directories);
        return ReturnSuccessfulResult();
    }

    /// <summary>
    /// Retrieves an item that is linked to the specified item.
    /// </summary>
    /// <param name="itemType">The type of the primary item.</param>
    /// <param name="linkedItemType">The type of the linked item.</param>
    /// <param name="itemId">The unique identifier of the primary item.</param>
    /// <param name="items">The output parameter containing the linked item.</param>
    /// <returns>An <see cref="IPLMResult"/> instance indicating the result of the operation.</returns>
    public IPLMResult GetLinkedItem(TPLMItemType itemType, TPLMItemType linkedItemType, string itemId, out IPLMTree? items)
    {
        if (string.IsNullOrEmpty(itemId) || itemType != TPLMItemType.itMachine || linkedItemType != TPLMItemType.itPostprocessor)
        {
            items = null;
            return new PLMResult {
                Code = 1,
                ErrorMessage = $"Cannot retrieve linked item with type {linkedItemType} for item with type {itemType}"
            };
        }
        
        string[] directories;
        if (itemId.Contains(emptyElementId))
            directories = [];
        else
            directories = [machinesDirectoryHelper?.FindSubdirectoryByExactName(itemId) ?? string.Empty];
        string postprocessorDir = string.Empty;
        foreach (var dir in directories)
            if (string.Equals(Path.GetFileName(dir), "Postprocessor"))
                postprocessorDir = dir;

        if (string.IsNullOrEmpty(postprocessorDir))
        {
            items = null;
            return new PLMResult {
                Code = 1,
                ErrorMessage = $"Cannot retrieve linked item for item with id {itemId}"
            };            
        }

        items = GetPLMTree(linkedItemType, [postprocessorDir]);
        return ReturnSuccessfulResult();
    }

    /// <summary>
    /// Retrieves the child items of a specified parent item.
    /// </summary>
    /// <param name="itemType">The type of the parent item.</param>
    /// <param name="parentItemId">The unique identifier of the parent item.</param>
    /// <param name="items">The output parameter containing the retrieved child items.</param>
    /// <returns>An <see cref="IPLMResult"/> instance indicating the result of the operation.</returns>
    public IPLMResult GetChilds(TPLMItemType itemType, string parentItemId, out IPLMTree? items)
    {
        string[] directories;
        var addNewElement = false;
        PLMDirectoryHelper? dirHelper;
        try
        {
            if (string.IsNullOrEmpty(parentItemId) || !parentItemId.Contains(emptyElementId))
            {
                switch (itemType)
                {
                    case TPLMItemType.itMachine:
                        dirHelper = machinesDirectoryHelper;
                        break;
                    case TPLMItemType.itPostprocessor:
                        dirHelper = postprocessorsDirectoryHelper;
                        break;
                    case TPLMItemType.itModel:
                    case TPLMItemType.itWorkpiece:
                        dirHelper = modelsDirectoryHelper;
                        break;
                    case TPLMItemType.itProject:
                        dirHelper = projectsDirectoryHelper;
                        break;
                    case TPLMItemType.itTool:
                        dirHelper = toolsDirectoryHelper;
                        break;
                    default:
                        items = new PLMTree();
                        return ReturnSuccessfulResult();
                }
                var parentDir = dirHelper?.FindSubdirectoryByExactName(parentItemId) ?? string.Empty;
                var parentDirType = GetDirectoryPLMItemType(itemType, parentDir);
                if (parentDirType != TPLMItemType.itNone)
                    directories = [];
                else
                    directories = dirHelper?.GetSubdirectories(parentItemId) ?? [];

                addNewElement = true;
            }
            else
                directories = [];
        }
        catch (Exception ex)
        {
            items = null;
            return new PLMResult
            {
                Code = 1,
                ErrorMessage = $"An exception occured while getting items from PLM. Exception message: {ex.Message}"
            };
        }
        
        items = GetPLMTree(itemType, directories, addNewElement, parentItemId);
        return ReturnSuccessfulResult();
    }

    /// <summary>
    /// Searches for items by name within the PLM extension.
    /// </summary>
    /// <param name="itemType">The type of the item to search for.</param>
    /// <param name="itemName">The name of the item to find.</param>
    /// <param name="items">The output parameter containing the found items.</param>
    /// <returns>An <see cref="IPLMResult"/> instance indicating the result of the operation.</returns>
    public IPLMResult FindItems(TPLMItemType itemType, string itemName, out IPLMTree items)
    {
        string[] directories;
        switch (itemType)
        {
            case TPLMItemType.itMachine:
                directories = machinesDirectoryHelper?.FindSubdirectoriesByPartialName(itemName) ?? [];
                break;
            case TPLMItemType.itPostprocessor:
                directories = postprocessorsDirectoryHelper?.FindSubdirectoriesByPartialName(itemName) ?? [];
                break;
            case TPLMItemType.itModel: case TPLMItemType.itWorkpiece:
                directories = modelsDirectoryHelper?.FindSubdirectoriesByPartialName(itemName) ?? [];
                break;
            case TPLMItemType.itProject:
                directories = projectsDirectoryHelper?.FindSubdirectoriesByPartialName(itemName) ?? [];
                break;
            case TPLMItemType.itTool:
                directories = toolsDirectoryHelper?.FindSubdirectoriesByPartialName(itemName) ?? [];
                break;
            default:
                items = new PLMTree();
                return ReturnSuccessfulResult();
        }
        
        directories = directories.Where(dir => GetDirectoryPLMItemType(itemType, dir) == itemType).ToArray();        
        items = GetPLMTree(itemType, directories);
        return ReturnSuccessfulResult();
    }

    /// <summary>
    /// Downloads specified items to a given path.
    /// </summary>
    /// <param name="items">The items to download.</param>
    /// <param name="downloadPath">The destination path for the downloaded items.</param>
    /// <param name="dwnItems">The output parameter containing information about the downloaded items.</param>
    /// <returns>An <see cref="IPLMResult"/> instance indicating the result of the operation.</returns>
    public IPLMResult DownloadItems(IPLMItems items, string downloadPath, out IPLMDataItems dwnItems)
    {
        var copiedItems = new PLMDataItems();
        try 
        {
            for (int i = 0; i < items.Count; i++)
            {
                string? destFilePath;
                string directory;
                if (items[i].Id.Contains(emptyElementId))
                    continue;
                    
                switch (items[i].Type)
                {
                    case TPLMItemType.itMachine:
                        directory = machinesDirectoryHelper?.FindSubdirectoryByExactName(items[i].Id) ?? string.Empty;
                        destFilePath = machinesDirectoryHelper?.CopyFilesFromPLMDirectory(items[i].Id, downloadPath).FirstOrDefault();
                        break;
                    case TPLMItemType.itPostprocessor:
                        directory = machinesDirectoryHelper?.FindSubdirectoryByExactName(items[i].Id) ?? string.Empty;
                        destFilePath = postprocessorsDirectoryHelper?.CopyFilesFromPLMDirectory(items[i].Id, downloadPath).FirstOrDefault();
                        break;
                    case TPLMItemType.itModel:
                    case TPLMItemType.itWorkpiece:
                        directory = machinesDirectoryHelper?.FindSubdirectoryByExactName(items[i].Id) ?? string.Empty;
                        destFilePath = modelsDirectoryHelper?.CopyFilesFromPLMDirectory(items[i].Id, downloadPath).FirstOrDefault();
                        break;
                    case TPLMItemType.itTool:
                        directory = machinesDirectoryHelper?.FindSubdirectoryByExactName(items[i].Id) ?? string.Empty;
                        destFilePath = toolsDirectoryHelper?.CopyFilesFromPLMDirectory(items[i].Id, downloadPath).FirstOrDefault();
                        break;
                    default:
                        continue;
                }

                if (destFilePath is null)
                    continue;

                var creationDate = GetCreationTime(directory);
                var dwnDataItem = new PLMDataItem
                {
                    Id = items[i].Id,
                    Name = items[i].Id,
                    Type = items[i].Type,
                    TimeStamp = creationDate?.ToOADate() ?? default
                };

                dwnDataItem.AddFile(destFilePath);
                copiedItems.AddDataItem(dwnDataItem);
            }
        }
        catch (Exception ex)
        {
            dwnItems = new PLMDataItems();
            return new PLMResult {
                Code = 1,
                ErrorMessage = $"An exception occured while downloading items from PLM. Exception message: {ex.Message}"
            };
        }
        
        dwnItems = copiedItems;
        return ReturnSuccessfulResult();
    }

    /// <summary>
    /// Downloads a project and retrieves its structure.
    /// </summary>
    /// <param name="itemId">The unique identifier of the project.</param>
    /// <param name="downloadPath">The destination path where the project will be saved.</param>
    /// <param name="dwnItems">The output parameter containing the downloaded project files.</param>
    /// <param name="prjStructItems">The output parameter containing the project structure items.</param>
    /// <returns>An <see cref="IPLMResult"/> instance indicating the result of the operation.</returns>
    public IPLMResult DownloadProject(string itemId, string downloadPath, out IPLMDataItems dwnItems, out IPLMProjectStructItems prjStructItems)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            dwnItems = new PLMDataItems();
            prjStructItems = new PLMProjectStructItems();
            return new PLMResult {
                Code = 1,
                ErrorMessage = "The item identificator is empty"
            };
        }

        if (itemId.Contains(emptyElementId))
        {
            dwnItems = new PLMDataItems();
            prjStructItems = new PLMProjectStructItems();
            return new PLMResult {
                Code = 1,
                ErrorMessage = "The item is empty"
            };
        }

        var copiedItems = new PLMDataItems();
        var structItems = new PLMProjectStructItems();
            
        try
        {
            var dwnFiles = projectsDirectoryHelper?.CopyFilesFromPLMDirectory(itemId, downloadPath, false) ?? [];

            var dwnDataItem = new PLMDataItem
            {
                Id = itemId,
                Name = itemId,
                Type = TPLMItemType.itProject
            };

            dwnDataItem.AddFiles(dwnFiles);
            copiedItems.AddDataItem(dwnDataItem);
        }
        catch (Exception ex)
        {
            dwnItems = new PLMDataItems();
            prjStructItems = new PLMProjectStructItems();
            return new PLMResult
            {
                Code = 1,
                ErrorMessage = $"An exception occured while downloading project from PLM. Exception message: {ex.Message}"
            };
        }

        dwnItems = copiedItems;
        prjStructItems = structItems;
        return ReturnSuccessfulResult();
    }

    /// <summary>
    /// Uploads a project to the PLM extension.
    /// </summary>
    /// <param name="project">The project to upload.</param>
    /// <param name="saveAs">A boolean indicating whether to save the project as a new instance.</param>
    /// <param name="replace">A boolean indicating whether to replace an existing project.</param>
    /// <returns>An <see cref="IPLMResult"/> instance indicating the result of the operation.</returns>
    public IPLMResult UploadProject(IPLMCAMProject project, bool saveAs, bool replace)
    {
        try
        {
            List<string> items = [];
            for (var i = 0; i < project.ProjectFiles.Count; i++)
                items.Add(project.ProjectFiles[i].FileName);

            var projectId = project.Id;
            if (project.Id.Contains(emptyElementId))
            {
                projectId = $"Project_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
                string parentPath = project.Id.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
                projectsDirectoryHelper?.CreateDirectory(parentPath, projectId);
            }

            projectsDirectoryHelper?.CopyFilesToPLMDirectory(projectId, items, replace);

            // List<IPLMOperation> operations = [];
            // for (var i = 0; i < project.OperationList.Count; i++)
            // {
            //     operations.Add(project.OperationList[i]);
            // }
            // var operationsString = JsonSerializer.Serialize(operations);
            // projectsDirectoryHelper.AddJsonFile(project.Id, "Operations", operationsString);

            // List<IPLMTool> tools = [];
            // for (var i = 0; i < project.ToolList.Count; i++)
            // {
            //     tools.Add(project.ToolList[i]);
            // }
            // var toolsString = JsonSerializer.Serialize(tools);
            // projectsDirectoryHelper.AddJsonFile(project.Id, "Tools", toolsString);
        }
        catch (Exception ex)
        {
            return new PLMResult {
                Code = 1,
                ErrorMessage = $"An exception occured while uploading project to PLM. Exception message: {ex.Message}"
            };
        }

        return ReturnSuccessfulResult();
    }

    /// <summary>
    /// Uploads an item with associated files and attributes to the PLM extension.
    /// </summary>
    /// <param name="itemType">The type of the item being uploaded.</param>
    /// <param name="itemId">The unique identifier of the item.</param>
    /// <param name="files">The files associated with the item.</param>
    /// <param name="itemAttributes">The attributes associated with the item.</param>
    /// <param name="replace">A boolean indicating whether to replace an existing item.</param>
    /// <param name="uplItems">The output parameter containing information about the uploaded items.</param>
    /// <returns>An <see cref="IPLMResult"/> instance indicating the result of the operation.</returns>
    public IPLMResult UploadItem(TPLMItemType itemType, string itemId, IPLMFiles files, IPLMItemAttributes itemAttributes, bool replace, out IPLMDataItems uplItems)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            uplItems = new PLMDataItems();
            return new PLMResult {
                Code = 1,
                ErrorMessage = "The item identificator is empty"
            };
        }

        var copiedItems = new PLMDataItems();
        try
        {
            switch (itemType)
            {
                case TPLMItemType.itMachine:
                    var machDirectory = itemId;
                    if (itemId.Contains(emptyElementId))
                    {
                        string parentPath = itemId.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
                        machDirectory = Path.GetFileNameWithoutExtension(files[0]);
                        machinesDirectoryHelper?.CreateDirectory(parentPath, machDirectory);
                    }
                    machinesDirectoryHelper?.CopyFileToPLMDirectory(files[0], machDirectory, replace);
                    break;
                case TPLMItemType.itPostprocessor:
                    var ppDirectory = itemId;
                    if (itemId.Contains(emptyElementId))
                    {
                        string parentPath = itemId.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
                        ppDirectory = Path.GetFileNameWithoutExtension(files[0]);
                        postprocessorsDirectoryHelper?.CreateDirectory(parentPath, ppDirectory);
                    }
                    postprocessorsDirectoryHelper?.CopyFileToPLMDirectory(files[0], ppDirectory, replace);
                    break;
                case TPLMItemType.itTool:
                    var toolDirectory = itemId;
                    if (itemId.Contains(emptyElementId))
                    {
                        string parentPath = itemId.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
                        toolDirectory = Path.GetFileNameWithoutExtension(files[0]);
                        toolsDirectoryHelper?.CreateDirectory(parentPath, toolDirectory);
                    }
                    toolsDirectoryHelper?.CopyFileToPLMDirectory(files[0], toolDirectory, replace);
                    break;
                default:
                    break;
            }
                
            copiedItems.AddDataItem(itemId, itemId, itemType);
        }
        catch (Exception ex)
        {
            uplItems = new PLMDataItems();
            return new PLMResult {
                Code = 1,
                ErrorMessage = $"An exception occured while uploading items to PLM. Exception message: {ex.Message}"
            };
        }

        uplItems = copiedItems;
        return ReturnSuccessfulResult();
    }

    /// <summary>
    /// Retrieves detailed data for a specific item in the PLM extension.
    /// </summary>
    /// <param name="itemType">The type of the item.</param>
    /// <param name="itemId">The unique identifier of the item.</param>
    /// <param name="itemData">The output parameter containing the retrieved item data.</param>
    /// <returns>An <see cref="IPLMResult"/> instance indicating the result of the operation.</returns>
    public IPLMResult GetItemData(TPLMItemType itemType, string itemId, out IPLMDataItem itemData) 
    {
        if (string.IsNullOrEmpty(itemId))
        {
            itemData = new PLMDataItem();
            return new PLMResult {
                Code = 1,
                ErrorMessage = "The item identificator is empty"
            };
        }

        string directory;
        switch (itemType)
        {
            case TPLMItemType.itMachine:
                directory = machinesDirectoryHelper?.FindSubdirectoryByExactName(itemId) ?? string.Empty;
                break;
            case TPLMItemType.itPostprocessor:
                directory = postprocessorsDirectoryHelper?.FindSubdirectoryByExactName(itemId) ?? string.Empty;
                break;
            default:
                directory = string.Empty;
                break;
        }

        if (directory == string.Empty)
        {
            itemData = new PLMDataItem();
            return new PLMResult {
                Code = 1,
                ErrorMessage = $"The item {itemId} not found"
            };
        }

        var creationDate = GetCreationTime(directory);
        var dirType = GetDirectoryPLMItemType(itemType, directory);
        itemData = new PLMDataItem
                {
                    Id = itemId,
                    Name = itemId,
                    Type = dirType,
                    TimeStamp = creationDate?.ToOADate() ?? default
                };
        return ReturnSuccessfulResult();
    }

    private IPLMResult ReturnSuccessfulResult()
    {
        var result = new PLMResult();
        result.SetSuccessful();
        return result;
    }

    private PLMTree GetPLMTree(TPLMItemType itemType, string[] directories, bool addNewElement = false, string parentItemId = "")
    {
        var plmTree = new PLMTree();
        foreach (var directory in directories)
        {
            var directoryName = Path.GetFileName(directory);
            var dirType = GetDirectoryPLMItemType(itemType, directory);
            plmTree.AddTreeItem(directoryName, directoryName, string.Empty, dirType);
        }

        if (addNewElement)
            plmTree.AddTreeItem($"{parentItemId}/{emptyElementId}", "New element", string.Empty, itemType);

        return plmTree;
    }

    private TPLMItemType GetDirectoryPLMItemType(TPLMItemType itemType, string itemDirectory)
    {
        var resultType = TPLMItemType.itNone;
        if (Directory.Exists(itemDirectory))
            if (!Directory.EnumerateFileSystemEntries(itemDirectory).Any())
                resultType = itemType;
            else switch (itemType)
            {
                case TPLMItemType.itMachine:
                    if (Directory.EnumerateFiles(itemDirectory, "*.xml").Any())
                        resultType = itemType;
                    break;
                case TPLMItemType.itPostprocessor:
                    string[] ppExtensions = [".spp", ".sppx", ".dll", ".csproj", ".cs", ".xml", ".json", ".cmd", ".xsd"];
                    if (Directory.EnumerateFiles(itemDirectory)
                            .Any(file => ppExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)))
                        resultType = itemType;
                    break;
                case TPLMItemType.itModel: case TPLMItemType.itWorkpiece:
                    string[] modelExtensions = [".igs", ".3dm", ".stp", ".eps"];
                    if (Directory.EnumerateFiles(itemDirectory)
                            .Any(file => modelExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase)))
                        resultType = itemType;
                    break;
                case TPLMItemType.itProject:
                    if (Directory.EnumerateFiles(itemDirectory).Any())
                        resultType = itemType;
                    break;
                case TPLMItemType.itTool:
                    if (Directory.EnumerateFiles(itemDirectory).Any())
                        resultType = itemType;
                    break;
                default:
                    break;
            }

        return resultType;
    }

    private DateTime? GetCreationTime(string directory)
    {
        DateTime creationDate;
        try
        {
            creationDate = Directory.GetCreationTime(directory);
        }
        catch (Exception)
        {
            return null;
        }

        return creationDate;
    }
}
