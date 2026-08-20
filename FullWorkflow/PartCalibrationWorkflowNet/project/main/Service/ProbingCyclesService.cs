using CAMAPI.DotnetHelper;
using CAMAPI.GeomModel;
using CAMAPI.ModelFormerTypes;
using CAMAPI.Project;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;
using CAMAPI.Technologist;
using PartCalibrationWorkflowNet.Model;
using STTypes;

namespace PartCalibrationWorkflowNet.Service;

/// <summary>
/// Tab 2: adds probing cycles to the chosen (or auto-created) measurement
/// operation using the points currently selected on the Model page.
/// </summary>
internal static class ProbingCyclesService
{
    public enum CycleType
    {
        SurfaceCycle,
        BossCycle,
        HoleCycle,
        WebCycle,
        GrooveCycle,
    }

    // Probe stand-off distance. Generous so the approach starts clear of the
    // material (too small a clearance makes the cycle invalid → red operation).
    private const double DefaultClearance = 25.0;
    private const double FaceTessTol      = 0.5;

    /// <summary>
    /// Snapshot of an enumerated probing-capable operation, returned to the UI.
    /// </summary>
    public sealed record OperationInfo(string Id, string Caption);

    /// <summary>
    /// List operations whose job-assignment former implements
    /// <see cref="ICamApiModelFormerWithProbingItems"/>.
    /// </summary>
    public static List<OperationInfo> EnumerateProbingOperations(
        ComWrapper<ICamApiProject> projCom)
    {
        var result = new List<OperationInfo>();
        using var techCom = projCom.Technologist();
        using var opsIt   = techCom.GetOperations(TCamApiReorderingMode.rmReordered);
        foreach (var opCom in opsIt.AsEnumerable())
        {
            bool isProbing;
            try
            {
                using var mfCom = opCom.ModelFormerJobAssignment();
                isProbing = mfCom.Invoke(mf => mf is ICamApiModelFormerWithProbingItems);
            }
            catch
            {
                continue;
            }
            if (!isProbing) continue;
            result.Add(new OperationInfo(opCom.Id(), opCom.Invoke(op => op.Name)));
        }
        return result;
    }

    /// <summary>
    /// Add probing cycles for every selected point on the Model page.
    /// If <paramref name="operationId"/> is null/empty, a new probing operation
    /// is created in the current setup. Surface normals are taken from the
    /// nearest triangle of the model geometry.
    /// </summary>
    /// <returns>Number of cycles added.</returns>
    public static int AddCyclesFromSelectedPoints(
        ComWrapper<ICamApiProject> projCom,
        string? operationId,
        CycleType cycleType)
    {
        var selectedPoints = ReadSelectedPoints(projCom);
        if (selectedPoints.Count == 0)
            throw new InvalidOperationException(
                "No points are selected on the Model page. Select probing points and try again.");

        var normals = ComputeSurfaceNormals(projCom, selectedPoints);

        using var techCom = projCom.Technologist();
        ComWrapper<ICamApiTechOperation> opCom = string.IsNullOrEmpty(operationId)
            ? CreateProbingOperation(techCom)
            : FindOperationById(techCom, operationId!)
              ?? throw new InvalidOperationException(
                  $"Probing operation '{operationId}' was not found in the project.");

        using (opCom)
        using (var mfCom = opCom.ModelFormerJobAssignment())
        {
            mfCom.Invoke(mf =>
            {
                if (mf is not ICamApiModelFormerWithProbingItems pmf)
                    throw new InvalidOperationException(
                        "Chosen operation does not support probing items.");

                int featureNum = 1;
                for (int i = 0; i < selectedPoints.Count; i++)
                {
                    var p = selectedPoints[i];
                    var n = normals[i];
                    AddCycle(pmf, cycleType, p, n, featureNum++);
                }
            });
        }
        return selectedPoints.Count;
    }

    // ── Cycle dispatcher ─────────────────────────────────────────────────

    private static void AddCycle(
        ICamApiModelFormerWithProbingItems pmf,
        CycleType cycleType,
        TST3DPoint point,
        TST3DPoint normal,
        int featureNum)
    {
        object cycle = cycleType switch
        {
            CycleType.SurfaceCycle => pmf.AddSurfaceCycle(),
            CycleType.BossCycle    => pmf.AddBossCycle(),
            CycleType.HoleCycle    => pmf.AddHoleCycle(),
            CycleType.WebCycle     => pmf.AddWebCycle(),
            CycleType.GrooveCycle  => pmf.AddGrooveCycle(),
            _ => throw new ArgumentOutOfRangeException(nameof(cycleType)),
        };

        // Direct member access is safe — we're inside the pmf.Invoke lambda.
        switch (cycle)
        {
            case ICamApiSurfaceProbingCycle surf:
                surf.TargetPoint  = point;
                surf.TargetVector = normal;
                surf.Clearance    = DefaultClearance;
                break;
            case ICamApiBossProbingCycle boss:
                boss.TargetPoint  = point;
                boss.TargetVector = normal;
                break;
            case ICamApiHoleProbingCycle hole:
                hole.TargetPoint  = point;
                hole.TargetVector = normal;
                break;
        }
        if (cycle is ICamApiProbingCycle probing)
        {
            probing.Caption = $"calib_{featureNum:D3}";
            // Without a write-to-report action the operation is incomplete (red).
            var report = probing.AddWriteToReportAction();
            report.ComponentNumber = 1;
            report.FeatureNumber = featureNum;
        }
    }

    // ── Operation creation ─────────────────────────────────────────────────

    private static ComWrapper<ICamApiTechOperation> CreateProbingOperation(
        ComWrapper<ICamApiTechnologist> techCom)
    {
        // A probing op needs a Part + Setup to live in; auto-create if missing.
        EnsureSetupAndPart(techCom);

        int setups;
        using (var listCom = techCom.PartAndStageList())
            setups = listCom.SetupStagesCount();

        // Pick the first "Probing" operation type (log all types for diagnosis).
        var captions = new List<string>();
        string? typeId = null;
        foreach (var typeCom in techCom.EnumerateOperationTypes())
        {
            var caption = typeCom.Caption();
            captions.Add(caption);
            if (typeId is null && caption.Contains("Probing", StringComparison.OrdinalIgnoreCase))
                typeId = typeCom.Id();
        }
        DbgLog.Write($"CreateProbingOperation: setups={setups}, probingTypeId='{typeId}', " +
                     $"availableTypes=[{string.Join(" | ", captions)}]");

        if (typeId is null)
            throw new InvalidOperationException(
                "No probing operation type is available in this project.");

        var opCom = techCom.CreateOperation(typeId, "", "");
        DbgLog.Write($"CreateProbingOperation: CreateOperation('{typeId}') -> IsNull={opCom.IsNull}");
        // CreateOperation returns null (no error status) if nothing can host it.
        if (opCom.IsNull)
            throw new InvalidOperationException(
                "Could not create a probing operation — the current machine/project cannot host it " +
                $"(setups={setups}, type='{typeId}'). See the dbg log for available operation types.");
        return opCom;
    }

    /// <summary>
    /// Ensure the project has a technology Part and a Setup stage to host an
    /// operation. A setup stage can exist while there is no Part — and an
    /// operation needs a Part as its parent (creating one also makes it the
    /// current op), otherwise CreateOperation returns nil without an error.
    /// </summary>
    private static void EnsureSetupAndPart(ComWrapper<ICamApiTechnologist> techCom)
    {
        int stages, parts;
        using (var listCom = techCom.PartAndStageList())
        {
            stages = listCom.SetupStagesCount();
            parts = listCom.PartsCount();
        }
        DbgLog.Write($"EnsureSetupAndPart: stages={stages}, parts={parts}");

        if (parts == 0)
        {
            using var partCom = techCom.CreatePart(1);
            DbgLog.Write($"EnsureSetupAndPart: CreatePart -> IsNull={partCom.IsNull}");
        }

        using (var listCom = techCom.PartAndStageList())
            stages = listCom.SetupStagesCount();
        if (stages == 0)
        {
            using var setupCom = techCom.CreateSetupStage();
            DbgLog.Write($"EnsureSetupAndPart: CreateSetupStage -> IsNull={setupCom.IsNull}");
        }
    }

    private static ComWrapper<ICamApiTechOperation>? FindOperationById(
        ComWrapper<ICamApiTechnologist> techCom, string id)
    {
        using var opsIt = techCom.GetOperations(TCamApiReorderingMode.rmReordered);
        foreach (var opCom in opsIt.AsEnumerable())
        {
            if (opCom.Id() == id)
                return opCom;
        }
        return null;
    }

    // ── Selected-points + normals ──────────────────────────────────────────

    internal static List<TST3DPoint> ReadSelectedPoints(ComWrapper<ICamApiProject> projCom)
    {
        var result = new List<TST3DPoint>();
        using var geomCom = projCom.CAMAPIGeomModel();
        var identity = IdentityMatrix();
        foreach (var nodeCom in geomCom.EnumerateNodes())
        {
            if (!nodeCom.Invoke(n => n.Selected)) continue;
            using var entityCom = nodeCom.GeometryEntity();
            if (entityCom.IsNull) continue;
            if (entityCom.EntityType() != TCAMAPIGeometryEntityType.etPoint) continue;
            var box = entityCom.Invoke(e => e.GetBoundBox(identity, out _));
            result.Add(new TST3DPoint
            {
                X = box.Min.X, Y = box.Min.Y, Z = box.Min.Z
            });
        }
        return result;
    }

    /// <summary>
    /// Same as <see cref="ReadSelectedPoints"/> but also captures each point's
    /// tree node name, so an emulated measurement file can be re-imported under
    /// the SAME names as the originals (handy for the demo).
    /// </summary>
    internal static List<Point3D> ReadSelectedNamedPoints(ComWrapper<ICamApiProject> projCom)
    {
        var result = new List<Point3D>();
        using var geomCom = projCom.CAMAPIGeomModel();
        var identity = IdentityMatrix();
        foreach (var nodeCom in geomCom.EnumerateNodes())
        {
            if (!nodeCom.Invoke(n => n.Selected)) continue;
            using var entityCom = nodeCom.GeometryEntity();
            if (entityCom.IsNull) continue;
            if (entityCom.EntityType() != TCAMAPIGeometryEntityType.etPoint) continue;
            var box = entityCom.Invoke(e => e.GetBoundBox(identity, out _));
            result.Add(new Point3D
            {
                Name = nodeCom.Name(),
                X = box.Min.X, Y = box.Min.Y, Z = box.Min.Z
            });
        }
        return result;
    }

    /// <summary>
    /// For every probing point, find the nearest triangle across all faces in
    /// the project geometry and return its normal. Brute-force O(P*T) — fine
    /// for the probing-point counts a user would realistically pick.
    /// </summary>
    private static List<TST3DPoint> ComputeSurfaceNormals(
        ComWrapper<ICamApiProject> projCom,
        List<TST3DPoint> points)
    {
        var result = new List<TST3DPoint>(points.Count);
        var bestDist = new double[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            result.Add(new TST3DPoint { X = 0, Y = 0, Z = 1 });
            bestDist[i] = double.PositiveInfinity;
        }

        using var geomCom = projCom.CAMAPIGeomModel();
        foreach (var nodeCom in geomCom.EnumerateNodes())
        {
            using var entityCom = nodeCom.GeometryEntity();
            if (entityCom.IsNull) continue;
            if (entityCom.EntityType() != TCAMAPIGeometryEntityType.etFace) continue;
            entityCom.Invoke(entity =>
            {
                if (entity is not ICamApiFaceGeometryEntity faceEntity) return;
                using var faceCom = ComWrapper.Create(faceEntity.Face);
                using var meshCom = faceCom.GetMesh(FaceTessTol);
                meshCom.Invoke(mesh =>
                {
                    int triCount = mesh.GetTriangleCount();
                    for (int t = 0; t < triCount; t++)
                    {
                        var tri  = mesh.GetTriangle(t);
                        var n    = mesh.GetTriangleNormal(t);
                        var v0   = mesh.GetVertex(tri.X);
                        var v1   = mesh.GetVertex(tri.Y);
                        var v2   = mesh.GetVertex(tri.Z);
                        var c    = new TST3DPoint
                        {
                            X = (v0.X + v1.X + v2.X) / 3.0,
                            Y = (v0.Y + v1.Y + v2.Y) / 3.0,
                            Z = (v0.Z + v1.Z + v2.Z) / 3.0,
                        };
                        for (int i = 0; i < points.Count; i++)
                        {
                            double dx = c.X - points[i].X, dy = c.Y - points[i].Y, dz = c.Z - points[i].Z;
                            double d2 = dx * dx + dy * dy + dz * dz;
                            if (d2 < bestDist[i])
                            {
                                bestDist[i] = d2;
                                result[i]   = n;
                            }
                        }
                    }
                });
            });
        }
        return result;
    }

    private static TST3DMatrix IdentityMatrix() => new()
    {
        vX = new TST3DPoint { X = 1, Y = 0, Z = 0 },
        vY = new TST3DPoint { X = 0, Y = 1, Z = 0 },
        vZ = new TST3DPoint { X = 0, Y = 0, Z = 1 },
        vT = default,
        D  = 1
    };
}
