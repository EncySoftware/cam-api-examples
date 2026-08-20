using CAMAPI.Application;
using CAMAPI.ApplicationMainForm;
using CAMAPI.DotnetHelper;
using CAMAPI.Project;
using CAMAPI.Singletons;
using STTypes;

namespace WorkpieceMillingWorkflowNet;

/// <summary>
/// Operations over the model
/// </summary>
public static class ModelHelper
{
    /// <summary>
    /// Relative path to the part we import and machine
    /// </summary>
    private const string ImportFilePath = @"Milling_3D\Forming mould.3dm";

    /// <summary>
    /// Beginning of the name of the geometry node the imported part lands in
    /// </summary>
    private const string PartNodeNamePrefix = "Forming mould";

    /// <summary>
    /// Coordinate system the part is measured in and the base of the matrix the part is moved by
    /// </summary>
    private static readonly TST3DMatrix IdentityMatrix = new()
    {
        vX = new TST3DPoint { X = 1, Y = 0, Z = 0 },
        vY = new TST3DPoint { X = 0, Y = 1, Z = 0 },
        vZ = new TST3DPoint { X = 0, Y = 0, Z = 1 },
        vT = default,
        D = 1
    };

    /// <summary>
    /// Prepare the model: import a part
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

        // import the part
        var modelsFolder = pathsHelperCom.Invoke(pathsHelper => pathsHelper.ModelsFolder)
                           ?? throw new Exception("Cannot get models folder");
        var importFile = Path.Combine(modelsFolder, ImportFilePath);
        if (!File.Exists(importFile))
            throw new Exception($"Cannot find file to import: {importFile}");

        using var geomImporterCom = activeProjectCom.InvokeAndWrap(project => project.GeomImporter);
        geomImporterCom.Invoke(importer =>
        {
            importer.ImportFile(importFile, "Part", false);
        });

        PlacePartUnderOrigin(activeProjectCom);
    }

    /// <summary>
    /// Center the imported part by X and Y and lower it until its top boundary lies on Z = 0,
    /// so that it is clamped in the vise instead of standing where the CAD file put it
    /// </summary>
    private static void PlacePartUnderOrigin(ComWrapper<ICamApiProject> activeProjectCom)
    {
        using var geomModelCom = activeProjectCom.CAMAPIGeomModel();

        // EnumerateNodes disposes every node it hands out, so the node is only used inside the loop
        foreach (var nodeCom in geomModelCom.EnumerateNodes())
        {
            var nodeName = nodeCom.FullName().Split('\\').Last();
            if (!nodeName.StartsWith(PartNodeNamePrefix, StringComparison.Ordinal))
                continue;

            using var entityCom = nodeCom.GeometryEntity();
            var boundBox = entityCom.GetBoundBox(IdentityMatrix);

            var shift = IdentityMatrix;
            shift.vT = new TST3DPoint
            {
                X = -(boundBox.Min.X + boundBox.Max.X) / 2,
                Y = -(boundBox.Min.Y + boundBox.Max.Y) / 2,
                Z = -boundBox.Max.Z
            };
            geomModelCom.Transform(nodeCom, shift);
            return;
        }

        throw new Exception($"Cannot find imported part node starting with '{PartNodeNamePrefix}'");
    }
}
