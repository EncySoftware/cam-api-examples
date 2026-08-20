using CAMAPI.CoordinateSystem;
using CAMAPI.DotnetHelper;
using CAMAPI.GeomModel;
using CAMAPI.PartStage;
using CAMAPI.Project;
using CAMAPI.ResultStatus;
using CAMAPI.Workpiece;
using STTypes;

namespace PartCalibrationWorkflowNet.Service;

/// <summary>
/// Tab 5: applies the calibration matrix in one of three ways
/// (create LCS / move a PartStage / transform a 3D-model folder).
/// </summary>
internal static class ApplyResultService
{
    /// <summary>Selector for a part stage in the project.</summary>
    public sealed record PartStageRef(int PartIndex, int SetupStageIndex)
    {
        public override string ToString() =>
            $"Part {PartIndex} / Setup {SetupStageIndex}";
    }

    // ── LCS ────────────────────────────────────────────────────────────────

    public static void CreateLcs(
        ComWrapper<ICamApiProject> projCom,
        TST3DMatrix matrix,
        string lcsName)
    {
        if (string.IsNullOrWhiteSpace(lcsName))
            throw new ArgumentException("LCS name must not be empty.", nameof(lcsName));

        using var listCom = projCom.InvokeAndWrap(p => p.CoordinateSystems);
        listCom.Invoke(list =>
        {
            list.Add(lcsName, matrix, "", out var status);
            if (status.Code == TResultStatusCode.rsError)
                throw new Exception(status.Description);
        });
    }

    // ── PartStage offset ───────────────────────────────────────────────────

    public static List<PartStageRef> EnumeratePartStages(ComWrapper<ICamApiProject> projCom)
    {
        var result = new List<PartStageRef>();
        using var techCom = projCom.Technologist();
        using var listCom = techCom.PartAndStageList();
        int parts  = listCom.PartsCount();
        int stages = listCom.SetupStagesCount();
        for (int p = 0; p < parts; p++)
            for (int s = 0; s < stages; s++)
                result.Add(new PartStageRef(p, s));
        return result;
    }

    public static void MovePartStage(
        ComWrapper<ICamApiProject> projCom,
        TST3DMatrix matrix,
        PartStageRef target)
    {
        using var techCom    = projCom.Technologist();
        using var listCom    = techCom.PartAndStageList();
        if (target.PartIndex >= listCom.PartsCount())
            throw new InvalidOperationException(
                $"Part index {target.PartIndex} is out of range — project has only {listCom.PartsCount()} parts.");
        if (target.SetupStageIndex >= listCom.SetupStagesCount())
            throw new InvalidOperationException(
                $"Setup index {target.SetupStageIndex} is out of range — project has only {listCom.SetupStagesCount()} setups.");

        // Reuse Setup 1's workpiece CS name.
        string setup1CsName;
        using (var setup1Com   = listCom.GetPartStage(0, 0))
        using (var setup1WpCom = setup1Com.WorkpieceSetup())
            setup1CsName = setup1WpCom.WorkpieceSideCoordinateSystemName();

        using var partStageCom = listCom.GetPartStage(target.PartIndex, target.SetupStageIndex);

        // Express the calibration in Setup 2's own coordinate system: M·C2.
        var c2 = ResolveSetupCsMatrix(projCom, partStageCom);

        using var wpSetupCom   = partStageCom.WorkpieceSetup();
        wpSetupCom.SetWorkpieceSideCoordinateSystemName(setup1CsName);
        wpSetupCom.SetOffset(Multiply(matrix, c2));
    }

    // Matrix of the part-stage's own coordinate system (WorkpieceCoordinateSystem),
    // resolved via its named CS in the project; identity if it isn't a named CS.
    private static TST3DMatrix ResolveSetupCsMatrix(
        ComWrapper<ICamApiProject> projCom, ComWrapper<ICamApiPartStage> partStageCom)
    {
        using var wcsCom = partStageCom.WorkpieceCoordinateSystem();
        if (wcsCom.Mode() != TCamApiWorkpieceCoordinateSystemMode.wcsName)
            return IdentityMatrix;
        using var listCom = projCom.CoordinateSystems();
        using var csCom = listCom.GetByName(wcsCom.CoordinateSystemName());
        return csCom.IsNull ? IdentityMatrix : csCom.Matrix();
    }

    private static readonly TST3DMatrix IdentityMatrix = new()
    {
        vX = new TST3DPoint { X = 1 },
        vY = new TST3DPoint { Y = 1 },
        vZ = new TST3DPoint { Z = 1 },
        vT = default,
        D  = 1,
    };

    // Compose rigid transforms: result applied to a point = a(b(point)).
    private static TST3DMatrix Multiply(TST3DMatrix a, TST3DMatrix b) => new()
    {
        vX = Rotate(a, b.vX),
        vY = Rotate(a, b.vY),
        vZ = Rotate(a, b.vZ),
        vT = new TST3DPoint
        {
            X = a.vX.X * b.vT.X + a.vY.X * b.vT.Y + a.vZ.X * b.vT.Z + a.vT.X,
            Y = a.vX.Y * b.vT.X + a.vY.Y * b.vT.Y + a.vZ.Y * b.vT.Z + a.vT.Y,
            Z = a.vX.Z * b.vT.X + a.vY.Z * b.vT.Y + a.vZ.Z * b.vT.Z + a.vT.Z,
        },
        D = 1,
    };

    private static TST3DPoint Rotate(TST3DMatrix a, TST3DPoint v) => new()
    {
        X = a.vX.X * v.X + a.vY.X * v.Y + a.vZ.X * v.Z,
        Y = a.vX.Y * v.X + a.vY.Y * v.Y + a.vZ.Y * v.Z,
        Z = a.vX.Z * v.X + a.vY.Z * v.Y + a.vZ.Z * v.Z,
    };

    // ── Transform 3D model folder ──────────────────────────────────────────

    public static void Transform3DModel(
        ComWrapper<ICamApiProject> projCom,
        TST3DMatrix matrix,
        string folderFullName)
    {
        if (string.IsNullOrWhiteSpace(folderFullName))
            throw new ArgumentException(
                "Target 3D-model folder path must not be empty.", nameof(folderFullName));

        using var geomCom = projCom.CAMAPIGeomModel();
        using var nodeCom = geomCom.InvokeAndWrap(
            model => model.FindByFullName(folderFullName, out _));
        if (nodeCom.IsNull)
            throw new InvalidOperationException(
                $"Folder '{folderFullName}' was not found in the Model tree.");

        geomCom.Invoke(model =>
        {
            var status = model.Transform(nodeCom.Instance, matrix);
            if (status.Code == TResultStatusCode.rsError)
                throw new Exception(status.Description);
        });
    }
}
