using System.IO;
using CAMAPI.DotnetHelper;
using CAMAPI.Project;
using CAMAPI.ResultStatus;
using CAMAPI.Singletons;
using STTypes;

namespace PartCalibrationWorkflowNet.Service;

/// <summary>
/// Imports a list of named 3D points into the project's geometry tree as a visual group,
/// so they appear in the geometry model for inspection.
///
/// Pipeline (ported from ProbeSetupApp.Services.GeomVisualizationService):
///   ISTGeomFiler writes a temporary .sgf file → GeomImporter loads it into the project.
/// </summary>
internal static class GeomVisualizationService
{
    private const string GroupName = "probe_points";

    /// <summary>
    /// Writes <paramref name="points"/> to a temporary .sgf file and imports it into
    /// the project geometry tree as group "<see cref="GroupName"/>".
    /// </summary>
    public static void ImportPoints(
        ComWrapper<ICamApiProject> projCom,
        IEnumerable<(string Name, TST3DPoint Pos)> points)
    {
        var sgfPath = Path.Combine(Path.GetTempPath(), "probe_points.sgf");

        using var factoryCom = FactoryGeometryFileHelper.GetSingleton();
        using var geomFileCom = factoryCom.CreateObject();

        if (!geomFileCom.StartFile(sgfPath))
            throw new Exception($"GeomVisualizationService: cannot start SGF file: {sgfPath}");

        geomFileCom.WriteGeometry(r =>
        {
            r.SetCurrentTransform(
                new TST3DPoint { X = 0, Y = 0, Z = 0 },
                new TST3DPoint { X = 0, Y = 0, Z = 1 },
                new TST3DPoint { X = 1, Y = 0, Z = 0 });

            r.StartGroupEntity(GroupName);
            foreach (var (name, pos) in points)
            {
                r.CreatePoint(name, pos);
                r.AddEntity(name, name);
            }
            r.CloseGroupEntity();
            r.CloseModel();
        });

        geomFileCom.CloseFile();

        using var importerCom = projCom.GeomImporter();
        importerCom.ImportFile(sgfPath, "", false);
    }
}
