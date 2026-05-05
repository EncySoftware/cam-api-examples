using System.IO;
using System.Text.Json;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.ModelFormerTypes;
using CAMAPI.NCMaker;
using CAMAPI.Project;
using CAMAPI.ResultStatus;
using CAMAPI.Singletons;
using CAMAPI.Technologist;
using CAMAPI.TechOperation;
using PartCalibrationWorkflowNet.Model;
using PartCalibrationWorkflowNet.Repository;
using STTypes;

namespace PartCalibrationWorkflowNet.Service;

/// <summary>
/// Handles the "Prepare Project" phase of the calibration workflow.
/// Logic ported from ProbeSetupApp (SetupService, ProbePointsService,
/// GeometryMeshRepository, MeshExtremePointFinder, ProbingCyclesService).
/// </summary>
internal sealed class PrepareProjectService
{
    // ── Constants (mirror ProbeSetupApp.Program) ──────────────────────────────

    private const double ClearanceSide     = 25.0;
    private const double ClearanceTop      = 5.0;
    private const double ProbeMinHeightZ   = 10.0;
    private const double WorkpieceTxOffset = 20.0;

    private const string ModelPath = @"C:\repo\CAM_2\Models\Milling_3D\49-1.igs";

    private static readonly TST3DMatrix WorkpieceOffset = new TST3DMatrix
    {
        vX = new TST3DPoint { X =  0, Y = 0, Z = 1 },
        vY = new TST3DPoint { X =  0, Y = 1, Z = 0 },
        vZ = new TST3DPoint { X = -1, Y = 0, Z = 0 },
        vT = new TST3DPoint { X = WorkpieceTxOffset, Y = 0, Z = 0 },
        D  = 1
    };

    private readonly string _pluginDir;

    public PrepareProjectService(string pluginDir)
    {
        _pluginDir = pluginDir;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a fresh project: imports model, finds probe points, imports them as
    /// visible geometry, creates Setup 1 with casting workpiece and probing SurfaceCycles,
    /// creates Setup 2 with a part for WCS assignment, writes nominal.json.
    /// </summary>
    public void CreateProject(ComWrapper<ICamApiApplication> appCom)
    {
        appCom.CreateNewProject();
        using var projCom = appCom.GetActiveProject()
            ?? throw new Exception("Failed to get active project after CreateNewProject");
        
        // 1. Import model
        if (!File.Exists(ModelPath))
            throw new FileNotFoundException($"Model not found: {ModelPath}");
        using var importerCom = projCom.GeomImporter();
        importerCom.ImportFile(ModelPath, "Part", false);
        
        // 2. Find probe points from geometry
        var probePoints = FindProbePoints(projCom);
        if (probePoints.Count == 0)
            throw new Exception("No probe points found in model geometry");
        
        // 3. Setup 1: create stage, set workpiece offset, add casting primitive
        using var techCom = projCom.Technologist();
        using var setup1Com = techCom.CreateSetupStage();
        
        using var partListCom = techCom.PartAndStageList();
        using var partStageCom = partListCom.GetPartStage(0, setup1Com.SetupStageIndex());
        using var wpSetupCom = partStageCom.WorkpieceSetup();
        wpSetupCom.SetOffset(WorkpieceOffset);
        
        using var setupAsOpCom = techCom.CurrentOperation();
        using var wpMfCom = setupAsOpCom.ModelFormerWorkpiece();
        wpMfCom.Invoke(wpMf =>
        {
            if (wpMf is not ICamApiModelFormerWithCastingPrimitive castingMf)
                throw new Exception("WorkpieceModelFormer does not implement ICamApiModelFormerWithCastingPrimitive");
        
            using var castingItemCom = ComWrapper.Create(castingMf.AddCasting(out var ret));
            if (ret.Code == TResultStatusCode.rsError)
                throw new Exception($"Failed to add casting primitive: {ret.Description}");
            castingItemCom.SetStock(0.0);
        });
        
        // 4. Create probing operation with SurfaceCycles
        var probingTypeId = FindProbingTypeId(techCom);
        using var opCom = techCom.CreateOperation(probingTypeId, "", "");
        using var mfCom = opCom.ModelFormerJobAssignment();
        mfCom.Invoke(mf =>
        {
            if (mf is not ICamApiModelFormerWithProbingItems pmf)
                throw new Exception("ModelFormer does not implement ICamApiModelFormerWithProbingItems");
            AddSurfaceCycles(pmf, probePoints);
        });
        
        // 5. Setup 2 + part (required so GetPartStage(0,1) works in calibration)
        using var setup2Com = techCom.CreateSetupStage();
        using var partCom = techCom.CreatePart(1);
        
        // 6. Write nominal.json
        var nominalPoints = probePoints.Select(e => new NominalPoint
        {
            X  = e.Point.ModelPosition.X, Y  = e.Point.ModelPosition.Y, Z  = e.Point.ModelPosition.Z,
            NX = e.Point.ModelNormal.X,   NY = e.Point.ModelNormal.Y,   NZ = e.Point.ModelNormal.Z,
        }).ToList();
        WriteNominalJson(_pluginDir, nominalPoints);
        
        // 7. Import probe points as visible geometry (after full project setup to avoid UI race)
        GeomVisualizationService.ImportPoints(projCom, probePoints.Select(e => (e.Name, e.Point.ModelPosition)));
    }

    /// <summary>
    /// Calculates toolpath for all operations, generates measurement.nc via the Fanuc postprocessor.
    /// Returns the full path to the generated NC file.
    /// </summary>
    public string GenerateNc(ComWrapper<ICamApiApplication> appCom)
    {
        using var projCom = appCom.GetActiveProject()
            ?? throw new Exception("No active project — run CreateProject first");
        using var techCom = projCom.Technologist();

        techCom.ResetAllOperationsToolpath();
        techCom.CalculateAllOperationsToolpath(true);

        var clDataFile = Path.Combine(_pluginDir, "measurement.inpcld");
        using var opsIter = techCom.GetOperations(TCamApiReorderingMode.rmReordered);
        projCom.SaveClData(clDataFile, opsIter);
        
        using var pathsExt = SystemExtensionFactory
            .GetSingletonExtension<ICamApiPaths>("Extension.Global.Singletons.Paths");
        var ppFile = Path.Combine(
            pathsExt.PostprocessorsFolder(),
            "Mill", "Fanuc (30i)_Mill.sppx");
        if (!File.Exists(ppFile))
            throw new FileNotFoundException($"Postprocessor not found: {ppFile}");
        
        using var ncMakerCom = projCom.NCMaker();
        using var settingsCom = ncMakerCom.CreateSettings(TCamApiNCMakerSettingsType.ncsSppx);
        settingsCom.Invoke(s =>
        {
            var sppx = (ICamApiMakeCncSppxSettings)s;
            sppx.OutputFolder = _pluginDir;
            sppx.NcFileName   = "measurement.nc";
        });
        using var generatedFiles = ncMakerCom.Generate(clDataFile, ppFile, settingsCom);
        
        return Path.Combine(_pluginDir, "measurement.nc");
    }

    // ── Point finding (ported from MeshExtremePointFinder + ProbePointsService) ──

    private static List<ProbePointEntry> FindProbePoints(ComWrapper<ICamApiProject> projCom)
    {
        var cloud = GeometryMeshRepository.BuildCloud(projCom);

        bool AboveFloor(SurfacePoint pt) => pt.WorldPosition.Z > ProbeMinHeightZ;
        bool AboveHalf(SurfacePoint pt)  => pt.WorldPosition.Z > ProbeMinHeightZ * 2;

        var sideDirections = new (string Name, TST3DPoint Dir, Func<SurfacePoint, bool>? Filter)[]
        {
            ("x_minus", new TST3DPoint { X = -1 }, AboveFloor),
            ("x_plus",  new TST3DPoint { X =  1 }, AboveHalf),
            ("y_minus", new TST3DPoint { Y = -1 }, null),
            ("y_plus",  new TST3DPoint { Y =  1 }, null),
        };

        var result = sideDirections
            .Select(s => new ProbePointEntry(
                s.Name,
                FindMostExtreme(cloud, s.Dir, s.Filter)
                    ?? throw new Exception($"No probe point found for direction {s.Name}")))
            .ToList();

        var topZones = FindTopZonePoints(cloud, zoneCount: 3);
        if (topZones.Count < 3)
            throw new Exception($"Not enough top zone points: {topZones.Count}");
        result.Add(new ProbePointEntry("z_left",  topZones[0]));
        result.Add(new ProbePointEntry("z_mid",   topZones[1]));
        result.Add(new ProbePointEntry("z_right", topZones[2]));

        return result;
    }

    // ── Extreme point finder (ported from MeshExtremePointFinder) ────────────

    private static SurfacePoint? FindMostExtreme(
        IEnumerable<SurfacePoint> cloud,
        TST3DPoint worldDirection,
        Func<SurfacePoint, bool>? preFilter = null)
    {
        SurfacePoint? best = null;
        double bestDot = double.MinValue;
        foreach (var pt in cloud)
        {
            if (preFilter != null && !preFilter(pt)) continue;
            double dot = pt.WorldPosition.X * worldDirection.X
                       + pt.WorldPosition.Y * worldDirection.Y
                       + pt.WorldPosition.Z * worldDirection.Z;
            if (dot > bestDot) { bestDot = dot; best = pt; }
        }
        return best;
    }

    private static List<SurfacePoint> FindTopZonePoints(
        IEnumerable<SurfacePoint> cloud,
        int zoneCount,
        double minNormalZ = 0.9)
    {
        var top = cloud.Where(pt => pt.WorldNormal.Z >= minNormalZ).ToList();
        if (top.Count == 0) return new List<SurfacePoint>();

        double wxMin = top.Min(pt => pt.WorldPosition.X);
        double wxMax = top.Max(pt => pt.WorldPosition.X);
        double zoneWidth = (wxMax - wxMin) / zoneCount;

        var result = new List<SurfacePoint>();
        for (int z = 0; z < zoneCount; z++)
        {
            double lo = wxMin + z * zoneWidth;
            double hi = z == zoneCount - 1 ? wxMax + 1 : wxMin + (z + 1) * zoneWidth;
            var pt = FindMostExtreme(
                top,
                new TST3DPoint { X = 0, Y = 0, Z = 1 },
                pt => pt.WorldPosition.X >= lo && pt.WorldPosition.X < hi);
            if (pt != null) result.Add(pt);
        }
        return result;
    }

    // ── Setup helpers ─────────────────────────────────────────────────────────

    private static string FindProbingTypeId(ComWrapper<ICamApiTechnologist> techCom)
    {
        foreach (var typeCom in techCom.EnumerateOperationTypes())
        {
            if (typeCom.Caption().Contains("Probing", StringComparison.OrdinalIgnoreCase))
                return typeCom.Id();
        }
        throw new Exception("No probing operation type found in this project");
    }

    private static void AddSurfaceCycles(
        ICamApiModelFormerWithProbingItems pmf,
        List<ProbePointEntry> points)
    {
        int featureNum = 1;
        foreach (var entry in points)
        {
            using var surfCom = ComWrapper.Create(pmf.AddSurfaceCycle());
            surfCom.SetTargetPoint(entry.Point.ModelPosition);
            surfCom.SetTargetVector(entry.Point.ModelNormal);
            surfCom.SetClearance(entry.Name.StartsWith("z_") ? ClearanceTop : ClearanceSide);

            surfCom.Invoke(surf =>
            {
                if (surf is not ICamApiProbingCycle cycle)
                    throw new Exception("SurfaceCycle does not implement ICamApiProbingCycle");
                cycle.Caption = entry.Name;

                using var reportCom = ComWrapper.Create(cycle.AddWriteToReportAction());
                reportCom.SetComponentNumber(1);
                reportCom.SetFeatureNumber(featureNum++);
            });
        }
    }

    private static void WriteNominalJson(string pluginDir, List<NominalPoint> points)
    {
        var path = Path.Combine(pluginDir, "nominal.json");
        File.WriteAllText(path, JsonSerializer.Serialize(
            new NominalData { ModelFile = "49-1.igs", Points = points },
            new JsonSerializerOptions { WriteIndented = true }));
    }
}
