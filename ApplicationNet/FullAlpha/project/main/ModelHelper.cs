using System;
using System.IO;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMIPC.Application;
using CAMIPC.ExecuteContext;
using CAMIPC.Project;

namespace ApplicationNetFullAlpha;

/// <summary>
/// Operations over the model
/// </summary>
public static class ModelHelper
{
    /// <summary>
    /// Relative path to the importing file
    /// </summary>
    private const string ImportFilePath = @"Milling_25D\Part1.igs";

    /// <summary>
    /// Prepare the model: import file
    /// </summary>
    public static void PrepareModel(ComWrapper<ICamIpcApplication> applicationCom,
        ComWrapper<ICamIpcProject> activeProjectCom,
        ComWrapper<ICamIpcPaths> pathsHelperCom)
    {
        // switch to model tab
        var executeContext = new TExecuteContext();
        applicationCom.Invoke(application =>
        {
            application.SetMainWorkMode(TMainWorkMode.mwmModel, ref executeContext);
            if (executeContext.ResultStatus.Code == CAMAPI.ResultStatus.TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
        });
        
        // import file
        ImportFile(activeProjectCom, pathsHelperCom);
    }

    /// <summary>
    /// Import a file to the project
    /// </summary>
    private static void ImportFile(ComWrapper<ICamIpcProject> projectCom,
        ComWrapper<ICamIpcPaths> pathsHelperCom)
    {
        // get the path to the file we will import
        var modelsFolder = pathsHelperCom.Invoke(pathsHelper => pathsHelper.ModelsFolder)
                           ?? throw new Exception("Cannot get models folder");
        var importFile = Path.Combine(modelsFolder, ImportFilePath);
        if (!File.Exists(importFile))
            throw new Exception($"Cannot find file to import: {importFile}");
        
        // import the file
        var executeContext = new TExecuteContext();
        using var geomImporterCom = projectCom.InvokeAndWrap(project => project.GetGeomImporter(ref executeContext));
        if (executeContext.ResultStatus.Code == CAMAPI.ResultStatus.TResultStatusCode.rsError)
            throw new Exception(executeContext.ResultStatus.Description);
        
        geomImporterCom.Invoke(importer =>
        {
            importer.ImportFile(importFile, "Part", false, ref executeContext);
            if (executeContext.ResultStatus.Code == CAMAPI.ResultStatus.TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
        });
    }
}