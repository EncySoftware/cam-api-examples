using System;
using System.Collections.Generic;
using System.Linq;
using CAMAPI.DotnetHelper;
using CAMAPI.Project;
using PartCalibrationWorkflowNet.Model;
using STTypes;

namespace PartCalibrationWorkflowNet.Service;

/// <summary>
/// Orchestrates Tab 4: read point coordinates from a Model-tree folder,
/// snap them to the surfaces currently selected on the viewport, run Kabsch
/// SVD, and return the matrix plus residual deviation.
/// </summary>
internal sealed class DeviationCalculationService
{
    private readonly CalibrationSolver _solver = new();

    public CalibrationResult Calculate(
        ComWrapper<ICamApiProject> projCom,
        string measuredFolderPath,
        string nominalFolderPath,
        SurfaceProjectionService projection)
    {
        var measured = GeomNodeLocator.ReadPointsFromFolder(projCom, measuredFolderPath);
        if (measured.Count < 3)
            throw new InvalidOperationException(
                $"Need at least 3 points in folder '{measuredFolderPath}'. Found {measured.Count}.");

        // Prefer exact correspondence by point name; fall back to surface snapping.
        if (!TryMatchByName(projCom, measured, nominalFolderPath, out var nominal, out var measuredArr))
        {
            measuredArr = measured
                .Select(p => new TST3DPoint { X = p.X, Y = p.Y, Z = p.Z })
                .ToArray();
            nominal = projection.SnapToModel(projCom, measuredArr);
        }

        var matrix = _solver.Solve(nominal, measuredArr);

        // Residual: max distance between R*nominal[i] + t and measured[i].
        double maxResidual = 0;
        for (int i = 0; i < measuredArr.Length; i++)
        {
            var transformed = Apply(matrix, nominal[i]);
            double dx = transformed.X - measuredArr[i].X;
            double dy = transformed.Y - measuredArr[i].Y;
            double dz = transformed.Z - measuredArr[i].Z;
            double d  = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (d > maxResidual) maxResidual = d;
        }

        return new CalibrationResult
        {
            Matrix      = matrix,
            MaxResidual = maxResidual,
            PointCount  = measuredArr.Length,
        };
    }

    /// <summary>
    /// Pair measured points to nominal points by name (Point1..N). Returns false
    /// (caller then falls back to surface snapping) when the nominal folder is
    /// empty or fewer than 3 names match.
    /// </summary>
    private static bool TryMatchByName(
        ComWrapper<ICamApiProject> projCom,
        List<Point3D> measured,
        string nominalFolderPath,
        out TST3DPoint[] nominal,
        out TST3DPoint[] measuredArr)
    {
        nominal = Array.Empty<TST3DPoint>();
        measuredArr = Array.Empty<TST3DPoint>();
        if (string.IsNullOrWhiteSpace(nominalFolderPath))
            return false;

        var byName = new Dictionary<string, Point3D>(StringComparer.OrdinalIgnoreCase);
        foreach (var n in GeomNodeLocator.ReadPointsFromFolder(projCom, nominalFolderPath))
            if (!string.IsNullOrEmpty(n.Name))
                byName[n.Name] = n;

        var nm = new List<TST3DPoint>();
        var mm = new List<TST3DPoint>();
        foreach (var m in measured)
        {
            if (!string.IsNullOrEmpty(m.Name) && byName.TryGetValue(m.Name, out var n))
            {
                nm.Add(new TST3DPoint { X = n.X, Y = n.Y, Z = n.Z });
                mm.Add(new TST3DPoint { X = m.X, Y = m.Y, Z = m.Z });
            }
        }
        if (nm.Count < 3)
            return false;

        nominal = nm.ToArray();
        measuredArr = mm.ToArray();
        return true;
    }

    private static TST3DPoint Apply(TST3DMatrix m, TST3DPoint p) => new()
    {
        X = m.vX.X * p.X + m.vY.X * p.Y + m.vZ.X * p.Z + m.vT.X,
        Y = m.vX.Y * p.X + m.vY.Y * p.Y + m.vZ.Y * p.Z + m.vT.Y,
        Z = m.vX.Z * p.X + m.vY.Z * p.Y + m.vZ.Z * p.Z + m.vT.Z,
    };
}
