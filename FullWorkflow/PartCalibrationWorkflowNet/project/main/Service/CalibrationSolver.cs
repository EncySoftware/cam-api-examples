using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using STTypes;

namespace PartCalibrationWorkflowNet.Service;

/// <summary>
/// Computes a rigid-body transformation (rotation + translation) that best maps
/// <c>nominal</c> points onto <c>measured</c> points using the Kabsch SVD algorithm.
/// </summary>
internal sealed class CalibrationSolver
{
    /// <summary>
    /// Finds rotation R and translation t such that R * nominal[i] + t ≈ measured[i].
    /// Requires at least 3 point pairs. Returns the result as a <see cref="TST3DMatrix"/>.
    /// </summary>
    public TST3DMatrix Solve(TST3DPoint[] nominal, TST3DPoint[] measured)
    {
        if (nominal.Length != measured.Length)
            throw new ArgumentException("CalibrationSolver: nominal and measured arrays must have equal length");
        if (nominal.Length < 3)
            throw new ArgumentException("CalibrationSolver: at least 3 point pairs are required");

        var (rotation, translation) = KabschSvd(nominal, measured);
        return BuildMatrix(rotation, translation);
    }

    private static (Matrix<double> R, TST3DPoint t) KabschSvd(TST3DPoint[] source, TST3DPoint[] target)
    {
        int n = source.Length;

        double csX = 0, csY = 0, csZ = 0, ctX = 0, ctY = 0, ctZ = 0;
        for (int i = 0; i < n; i++)
        {
            csX += source[i].X; csY += source[i].Y; csZ += source[i].Z;
            ctX += target[i].X; ctY += target[i].Y; ctZ += target[i].Z;
        }
        csX /= n; csY /= n; csZ /= n;
        ctX /= n; ctY /= n; ctZ /= n;

        var P = DenseMatrix.Build.Dense(n, 3);
        var Q = DenseMatrix.Build.Dense(n, 3);
        for (int i = 0; i < n; i++)
        {
            P[i, 0] = source[i].X - csX; P[i, 1] = source[i].Y - csY; P[i, 2] = source[i].Z - csZ;
            Q[i, 0] = target[i].X - ctX; Q[i, 1] = target[i].Y - ctY; Q[i, 2] = target[i].Z - ctZ;
        }

        var H = P.Transpose() * Q;
        var svd = H.Svd(computeVectors: true);
        var U = svd.U;
        var V = svd.VT.Transpose();

        var R = V * U.Transpose();
        if (R.Determinant() < 0)
        {
            var Vmod = V.Clone();
            for (int i = 0; i < 3; i++)
                Vmod[i, 2] = -Vmod[i, 2];
            R = Vmod * U.Transpose();
        }

        var cSrc = DenseVector.OfArray(new[] { csX, csY, csZ });
        var cTgt = DenseVector.OfArray(new[] { ctX, ctY, ctZ });
        var tVec = cTgt - R * cSrc;

        return (R, new TST3DPoint { X = tVec[0], Y = tVec[1], Z = tVec[2] });
    }

    private static TST3DMatrix BuildMatrix(Matrix<double> R, TST3DPoint t)
    {
        return new TST3DMatrix
        {
            vX = new TST3DPoint { X = R[0, 0], Y = R[1, 0], Z = R[2, 0] },
            vY = new TST3DPoint { X = R[0, 1], Y = R[1, 1], Z = R[2, 1] },
            vZ = new TST3DPoint { X = R[0, 2], Y = R[1, 2], Z = R[2, 2] },
            vT = t,
            A = 0, B = 0, C = 0, D = 1
        };
    }
}
