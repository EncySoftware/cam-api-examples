using System.IO;
using CAMAPI.DotnetHelper;
using CAMAPI.Project;
using CAMAPI.Singletons;
using Geometry.VecMatrLib;
using PartCalibrationWorkflowNet.Model;
using STGeomApiTypes;
using STTypes;

namespace PartCalibrationWorkflowNet.Service;

/// <summary>
/// Imports a list of 3D points into the project geometry tree as a named group.
///
/// Writes an SGF file via ISTGeomReceiver (StartGroupEntity → CreatePoint per
/// point → CloseGroupEntity), then GeomImporter.ImportFile pulls it in. This is
/// the same path the ExtensionUtilityGeomCustomImportNet example uses, and unlike
/// ICamApiGeometryModelSketcher.AddPoint (which hard-codes the Job-geometry
/// sub-tree and ignores ActiveNode) it actually lands the points inside the
/// chosen folder.
/// </summary>
internal static class GeomImportService
{
    /// <summary>
    /// Materialise <paramref name="points"/> as a group of geometric points in
    /// the project's Model tree. The group is named <paramref name="folderName"/>
    /// and placed under <paramref name="parentPath"/> (full Model-tree path of an
    /// existing node, or empty string for the current/root folder).
    /// </summary>
    public static void ImportPoints(
        ComWrapper<ICamApiProject> projCom,
        string parentPath,
        string folderName,
        IReadOnlyList<Point3D> points)
    {
        // Name the SGF after the target folder: ImportFile wraps the imported
        // content in a group named after the FILE, so the file name becomes the
        // single "<folderName>" folder (no extra SGF-named level). Use a unique
        // temp dir so concurrent imports of the same name don't collide.
        var safeName = folderName;
        foreach (var c in Path.GetInvalidFileNameChars())
            safeName = safeName.Replace(c, '_');
        if (string.IsNullOrWhiteSpace(safeName))
            safeName = "Points";
        var tempDir = Path.Combine(Path.GetTempPath(), "PartCalib_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var sgfPath = Path.Combine(tempDir, safeName + ".sgf");
        DbgLog.Write($"ImportPoints: writing {points.Count} points to SGF '{sgfPath}'");

        // 1. Write an SGF file with one named group of points. The filer is
        //    stateful and apartment-bound, so the whole sequence (create filer →
        //    StartFile → group → points → close) must run on ONE worker thread —
        //    do it inside a single Invoke.
        using (var factoryCom = SystemExtensionFactory.GetSingletonExtension<ICamApiFactoryGeometryFile>(
                   "Extension.Global.Singletons.GeomFile"))
        {
            factoryCom.Invoke(factory =>
            {
                var geomFile = factory.CreateObject()
                    ?? throw new Exception("Can't create geometry filer object.");
                if (!geomFile.StartFile(sgfPath))
                    throw new Exception("Can't start SGF file: " + sgfPath);
                if (geomFile is not ISTGeomReceiver receiver)
                    throw new Exception("Geometry filer does not implement ISTGeomReceiver.");
                try
                {
                    try
                    {
                        receiver.SetCurrentTransform(
                            T3DMatrix.Unit.vT, T3DMatrix.Unit.vZ, T3DMatrix.Unit.vX);
                        // No StartGroupEntity here: the import already wraps these
                        // in a group named after the file (== folderName).
                        for (var i = 0; i < points.Count; i++)
                        {
                            var name = string.IsNullOrWhiteSpace(points[i].Name)
                                ? $"Point{i + 1}"
                                : points[i].Name;
                            var pt = new TST3DPoint { X = points[i].X, Y = points[i].Y, Z = points[i].Z };
                            receiver.CreatePoint(name, pt);
                            receiver.AddEntity(name, name);
                        }
                    }
                    finally { receiver.CloseModel(); }
                }
                finally { geomFile.CloseFile(); }
            });
        }

        // 2. Import the SGF; TargetFolder == parentPath nests the new group there
        //    (empty == current/root folder).
        DbgLog.Write($"ImportPoints: importing SGF under '{parentPath}'");
        using var importerCom = projCom.GeomImporter();
        importerCom.ImportFile(sgfPath, parentPath, false);

        try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort temp cleanup */ }
        DbgLog.Write("ImportPoints: complete");
    }
}
