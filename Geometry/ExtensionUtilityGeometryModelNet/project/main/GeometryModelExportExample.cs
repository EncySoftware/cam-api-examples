using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.GeomModel;
using CAMAPI.GeomImporter;
using CAMAPI.ResultStatus;
using CAMAPI.Project;
using CAMAPI.Singletons;
using System.Runtime.InteropServices;

namespace ExtensionUtilityGeometryModelNet;

/// <summary>
/// Utility to export geometry node to .osd file
/// </summary>
public class GeometryModelExportExample : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext Context, out TResultStatus resultStatus)
    {        
        resultStatus = default;
        ICamApiProject? activeProject = null;

        try
        {
            // get global context
            var extension = Info?.InstanceInfo.ExtensionManager.GetSingletonExtension("Extension.Global.Singletons.Paths", out resultStatus)
                            ?? throw new Exception("Info is null");
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Error getting global context: " + resultStatus.Description);
            if (extension is not ICamApiPaths paths)
                throw new Exception("Cannot get global context");

            activeProject = Context.CamApplication.GetActiveProject(out resultStatus);
            
            if (resultStatus.Code == TResultStatusCode.rsSuccess)
            {
                var importFileName = Path.Combine(paths.ModelsFolder, "Milling_3D", "49-1.igs");
                var exportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CAMAPI Geometry examples");
                using var fullModel = new ApiComObject<ICAMAPIGeometryModel>(activeProject.CAMAPIGeomModel);
                using var importer = new ApiComObject<ICAMAPIGeometryImporter>(activeProject.GeomImporter);

                importer.Instance.ImportFile(importFileName, "", false);

                if(!Directory.Exists(exportDir))
                    Directory.CreateDirectory(exportDir);
                fullModel.Instance.ExportSelectedToOSD(Path.Combine(exportDir, "ExportedModelExample.osd"), out resultStatus);

                if (!(resultStatus.Code == TResultStatusCode.rsSuccess))
                    throw new Exception(resultStatus.Description);
            }
        } finally {
            if (activeProject != null)
                Marshal.ReleaseComObject(activeProject);
        }
    }
}
