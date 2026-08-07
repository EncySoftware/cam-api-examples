using System;
using System.IO;
using CAMAPI.Application;
using CAMAPI.ApplicationMainForm;
using CAMAPI.DotnetHelper;
using CAMAPI.Project;
using CAMAPI.Singletons;

namespace ApplicationFullWorkflow3DProjectNet;

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
    public static void PrepareModel(ComWrapper<ICamApiApplication> applicationCom,
        ComWrapper<ICamApiProject> activeProjectCom,
        ComWrapper<ICamApiPaths> pathsHelperCom)
    {
        // switch to model tab
        applicationCom.Invoke(application =>
        {
            application.MainWorkMode = TMainWorkMode.mwmModel;
        });
        
        // import file
        ImportFile(activeProjectCom, pathsHelperCom);
    }

    /// <summary>
    /// Import a file to the project
    /// </summary>
    private static void ImportFile(ComWrapper<ICamApiProject> projectCom, ComWrapper<ICamApiPaths> pathsHelperCom)
    {
        // get the path to the file we will import
        var modelsFolder = pathsHelperCom.Invoke(pathsHelper => pathsHelper.ModelsFolder)
                           ?? throw new Exception("Cannot get models folder");
        var importFile = Path.Combine(modelsFolder, ImportFilePath);
        if (!File.Exists(importFile))
            throw new Exception($"Cannot find file to import: {importFile}");
        
        // import the file
        using var geomImporterCom = projectCom.InvokeAndWrap(project => project.GeomImporter);
        geomImporterCom.Invoke(importer =>
        {
            importer.ImportFile(importFile, "Part", false);
        });
    }
}