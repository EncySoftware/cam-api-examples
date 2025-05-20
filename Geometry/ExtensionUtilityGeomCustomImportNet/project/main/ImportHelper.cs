using CAMAPI.Application;
using CAMAPI.DotnetHelper;

namespace ExtensionUtilityGeomCustomImportNet;

/// <summary>
/// Static class to execute duplicate code
/// </summary>
public static class ImportHelper
{
    /// <summary>
    /// Import sgf file into current active project
    /// </summary>
    public static void Import(IExtensionUtilityContext context, string sgfFilePath)
    {
        // active project
        using var applicationCom = ComWrapper.Create(context.CamApplication);
        using var activeProjectCom = applicationCom.InvokeAndWrap(application => 
            (application.GetActiveProject(out var status), status));
        
        // import the file
        activeProjectCom.Invoke(activeProject =>
        {
            using var geomImporterCom = ComWrapper.Create(activeProject.GeomImporter);
            geomImporterCom.Invoke(importer => 
                importer.ImportFile(sgfFilePath, "", true));
        });
    }
}