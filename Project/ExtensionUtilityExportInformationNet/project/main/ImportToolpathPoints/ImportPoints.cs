using System;
using System.IO;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.Singletons;
using Geometry.VecMatrLib;
using STGeomApiTypes;
using STTypes;

namespace ExtensionUtilityExportInformationNet;

/// <summary>
/// Extension to parse JSON toolpaths from OperationToolpathsJSON/Designed/ and create points in geometry model
/// </summary>
public class ImportToolpathPoints : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;

        try
        {
            var outputRoot = ExportOutputPaths.Root;
            var designedFolder = Path.Combine(outputRoot, "project", "main", "OperationToolpathsJSON", "Designed");
            var sgfFilePath = Path.Combine(outputRoot, "toolpath_points.sgf");

            if (!Directory.Exists(designedFolder))
                throw new Exception("Folder not found: " + designedFolder);

            var jsonFiles = Directory.GetFiles(designedFolder, "*.json");
            if (jsonFiles.Length == 0)
                throw new Exception("No JSON files found in: " + designedFolder);

            // Create geometry filer
            using var wrapperFactoryGeomFiler = SystemExtensionFactory.GetSingletonExtension<ICamApiFactoryGeometryFile>(
                "Extension.Global.Singletons.GeomFile");
            var factoryGeomFiler = wrapperFactoryGeomFiler.Instance
                                   ?? throw new Exception("Can't get geometry filer singleton");
            using var wrapperGeomFile = new ComWrapper<ISTGeomFiler>(factoryGeomFiler.CreateObject());
            var geomFile = wrapperGeomFile.Instance
                           ?? throw new Exception("Can't create geometry filer object");

            // Start SGF file
            if (!geomFile.StartFile(sgfFilePath))
                throw new Exception("Can't start file: " + sgfFilePath);

            if (geomFile is not ISTGeomReceiver geomReceiver)
                throw new Exception("Can`t cast geomFile to geomReceiver");
            try
            {
                geomReceiver.StartModel();
                try
                {
                    geomReceiver.SetCurrentTransform(T3DMatrix.Unit.vT, T3DMatrix.Unit.vZ, T3DMatrix.Unit.vX);

                    // Root group
                    geomReceiver.StartGroupEntity("ToolpathPoints");
                    try
                    {
                        foreach (var jsonFile in jsonFiles)
                        {
                            ToolpathPointsExporter.ProcessJsonFile(geomReceiver, jsonFile);
                        }
                    }
                    finally
                    {
                        geomReceiver.CloseGroupEntity();
                    }
                }
                finally
                {
                    geomReceiver.CloseModel();
                }
            }
            finally
            {
                geomFile.CloseFile();
            }

            // Import SGF into active project
            using var applicationCom = new ComWrapper<ICamApiApplication>(context.CamApplication);
            using var activeProjectCom = applicationCom.InvokeAndWrap(app =>
                app.GetActiveProject(out var ret));
            using var geomImporterCom = activeProjectCom.InvokeAndWrap(project => project.GeomImporter);
            geomImporterCom.Invoke(importer =>
            {
                var ret = importer.ImportFile(sgfFilePath, @"", true);
                if (ret.Code == TResultStatusCode.rsError)
                    throw new Exception("Can't import SGF file: " + ret.Description);
            });
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
