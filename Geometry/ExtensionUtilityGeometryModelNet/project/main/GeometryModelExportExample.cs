using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.GeomModel;
using CAMAPI.GeomImporter;
using CAMAPI.ResultStatus;
using CAMAPI.Project;
using CAMAPI.Singletons;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Geometry.VecMatrLib;

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
            var pathsCom = SystemExtensionFactory.GetSingletonExtension<ICamApiPaths>("Extension.Global.Singletons.Paths");
            var paths = pathsCom.Instance
                        ?? throw new Exception("Cannot get global context");

            activeProject = Context.CamApplication.GetActiveProject(out resultStatus);
            
            if (resultStatus.Code == TResultStatusCode.rsSuccess)
            {
                var importFileName = Path.Combine(paths.ModelsFolder, "Milling_3D", "49-1.igs");
                var exportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CAMAPI Geometry examples");
                var exportedFilePath = Path.Combine(exportDir, "ExportedModelExample.osd");
                using var fullModel = new ComWrapper<ICAMAPIGeometryModel>(activeProject.CAMAPIGeomModel);
                using var importer = new ComWrapper<ICAMAPIGeometryImporter>(activeProject.GeomImporter);

                importer.Instance.ImportFile(importFileName, "", false);

                if(!Directory.Exists(exportDir))
                    Directory.CreateDirectory(exportDir);
                if(File.Exists(exportedFilePath))
                    File.Delete(exportedFilePath);
                fullModel.Instance.ExportSelectedToOSD(exportedFilePath, out resultStatus);

                // Open the file in a new instance of Windows Explorer
                Process.Start("explorer.exe", "/select, " + exportedFilePath);

                if (!(resultStatus.Code == TResultStatusCode.rsSuccess))
                    throw new Exception(resultStatus.Description);

                // Import the exported file back into the CAM application
                importer.Instance.ImportFile(exportedFilePath, "", false);

                // Move the new model a little to place it next to the original model
                var geomNode = new ComWrapper<ICAMAPIGeometryTreeNode>(fullModel.Instance.ActiveNode);
                var shiftMatrix = T3DMatrix.MakeShiftMatrix(new T3DPoint { X = 0, Y = 0, Z = 100 });
                resultStatus = fullModel.Instance.Transform(geomNode.Instance, shiftMatrix);
            }
        } finally {
            if (activeProject != null)
                Marshal.ReleaseComObject(activeProject);
        }
    }
}
