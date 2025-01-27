using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.GeomImporter;
using CAMAPI.Project;
using CAMAPI.ResultStatus;
using CAMAPI.Singletons;
using STGeomApiTypes;

namespace ExtensionUtilityGeomCustomImportNet;

/// <summary>
/// Static class to execute duplicate code
/// </summary>
public static class ImportHelper
{
    /// <summary>
    /// Create instance, which will create all new geometry objects
    /// </summary>
    public static ISTGeomFiler GetImporter(string sgfFilePath, IExtensionInfo? info)
    {
        using var wrapperFactoryGeomFiler = SystemExtensionFactory.GetSingletonExtension<ICamApiFactoryGeometryFile>("Extension.Global.Singletons.GeomFile");
        var factoryGeomFiler = wrapperFactoryGeomFiler.Instance
                               ?? throw new Exception("Can't get geometry filer singleton");

        using var wrapperGeomFile = new ComWrapper<ISTGeomFiler>(factoryGeomFiler.CreateObject());
        var geomFile = wrapperGeomFile.Instance
                       ?? throw new Exception("Can't create geometry filer object");
            
        // beginning of the file
        if (!geomFile.StartFile(sgfFilePath))
            throw new Exception("Can't start file: " + sgfFilePath);
        
        return geomFile;
    }

    /// <summary>
    /// Import sgf file into current active project
    /// </summary>
    public static void Import(IExtensionUtilityContext context, string sgfFilePath)
    {
        // active project
        using var applicationCom = new ComWrapper<ICamApiApplication>(context.CamApplication);
        var application = applicationCom.Instance
                          ?? throw new Exception("Can't get application");
        using var activeProjectCom =
            new ComWrapper<ICamApiProject>(application.GetActiveProject(out var resultStatus));
        if (resultStatus.Code == TResultStatusCode.rsError)
            throw new Exception("Can't get active project: " + resultStatus.Description);
        var activeProject = activeProjectCom.Instance
                            ?? throw new Exception("Active project is null");

        // import the file
        using var geomImporterCom = new ComWrapper<ICAMAPIGeometryImporter>(activeProject.GeomImporter);
        var importer = geomImporterCom.Instance
                       ?? throw new Exception("Can't get geometry importer");
        resultStatus = importer.ImportFile(sgfFilePath, @"", true);
        if (resultStatus.Code == TResultStatusCode.rsError)
            throw new Exception("Can't import file: " + resultStatus.Description);
    }
}