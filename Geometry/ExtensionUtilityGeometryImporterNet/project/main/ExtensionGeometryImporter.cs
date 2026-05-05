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
            using var pathsHelperCom = SystemExtensionFactory.GetSingletonExtension<ICamApiPaths>("Extension.Global.Singletons.Paths");
            using var applicationCom = new ComWrapper<ICamApiApplication>(context.CamApplication);
            var modelFileName = pathsHelperCom.Invoke(pathsHelper => Path.Combine(pathsHelper.ModelsFolder, "Milling_3D", "Part.igs"));

            using var activeProjectCom = applicationCom.InvokeAndWrap(application =>
                (application.GetActiveProject(out var status), status));
            if (activeProjectCom == null)
                throw new Exception("Active project is not found");

            using var geomImporterCom = activeProjectCom.InvokeAndWrap(project => project.GeomImporter);
            geomImporterCom.Invoke(importer =>
            {
                var resultStatus = importer.ImportFile(modelFileName, @"", true);
                if (resultStatus.Code == TResultStatusCode.rsError)
                    throw new Exception("Can't import file: " + resultStatus.Description);
            });
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
