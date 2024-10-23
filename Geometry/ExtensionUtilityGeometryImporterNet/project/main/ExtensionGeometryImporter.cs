using System.Runtime.InteropServices;
using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.GeomImporter;
using CAMAPI.Project;
using CAMAPI.ResultStatus;
using CAMAPI.Singletons;
using CAMAPI.DotnetHelper;

namespace ExtensionUtilityGeometryImporterNet;

/// <summary>
/// Utility to import geometry from Milling_25D\Part1.igs into the active project
/// </summary>
public class ExtensionGeometryImporter: IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            using var pathsHelper = SystemExtensionFactory.GetSingletonExtension<ICamApiPaths>("Extension.Global.Singletons.Paths", Info);
            using var applicationCom = new ApiComObject<ICamApiApplication>(context.CamApplication);
            var application = applicationCom.Instance;

            // active project
            using var activeProjectCom =
                new ApiComObject<ICamApiProject>(application.GetActiveProject(out resultStatus));
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Can't get active project: " + resultStatus.Description);
            if (activeProjectCom == null)
                throw new Exception("Active project is not found");
            var activeProject = activeProjectCom.Instance;

            // geometry importer
            using var geomImporterCom = new ApiComObject<ICAMAPIGeometryImporter>(activeProject.GeomImporter);
            var importer = geomImporterCom.Instance;

            var modelFileName = Path.Combine(pathsHelper.Instance.ModelsFolder, "Milling_25D", "Part1.igs");
            resultStatus = importer.ImportFile(modelFileName, @"", true);
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Can't import file: " + resultStatus.Description);
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
