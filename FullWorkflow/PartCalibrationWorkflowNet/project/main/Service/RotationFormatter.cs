using System.Globalization;
using System.Text;
using PartCalibrationWorkflowNet.Model;
using STTypes;

namespace PartCalibrationWorkflowNet.Service;

/// <summary>
/// Renders a <see cref="TST3DMatrix"/> in the format chosen by the user
/// (Matrix4x4 / Euler XYZ / ZYX / ZXZ / X'Y'Z' / Quaternion).
///
/// Algorithms mirror STLibraries/STLib/EulerAngles.pas and
/// DotNet/VecMatrLib/EulerConverter.cs but are reimplemented locally to keep
/// the plugin self-contained (no extra Reference outside published CAMAPI).
/// </summary>
internal static class RotationFormatter
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Format(TST3DMatrix m, RotationFormat format)
    {
        return format switch
        {
            RotationFormat.Matrix4x4   => FormatMatrix(m),
            RotationFormat.EulerXYZ    => FormatFixedAxes(m, axisOrder: "XYZ"),
            RotationFormat.EulerZYX    => FormatFixedAxes(m, axisOrder: "ZYX"),
            RotationFormat.EulerZXZ    => FormatProperEulerZXZ(m),
            RotationFormat.EulerXpYpZp => FormatRotatingAxesXYZ(m),
            RotationFormat.Quaternion  => FormatQuaternion(m),
            _ => FormatMatrix(m),
        };
    }

    // ── Matrix 4x4 ────────────────────────────────────────────────────────

    private static string FormatMatrix(TST3DMatrix m)
    {
        var sb = new StringBuilder();
        var rows = new[]
        {
            new[] { m.vX.X, m.vY.X, m.vZ.X, m.vT.X },
            new[] { m.vX.Y, m.vY.Y, m.vZ.Y, m.vT.Y },
            new[] { m.vX.Z, m.vY.Z, m.vZ.Z, m.vT.Z },
            new[] { 0.0,    0.0,    0.0,    1.0    },
        };
        foreach (var row in rows)
            sb.AppendLine(string.Join("  ", row.Select(v => v.ToString("F6", Inv).PadLeft(12))));
        return sb.ToString().TrimEnd();
    }

    // ── Fixed-axes Euler (XYZ / ZYX) ──────────────────────────────────────

    /// <summary>
    /// Fixed-axes (extrinsic) Euler decomposition. The composed rotation is
    /// applied right-to-left in <paramref name="axisOrder"/>: e.g. for "XYZ"
    /// R = Rx(rx) * Ry(ry) * Rz(rz). The XYZ branch is robust around |ry|=π/2.
    /// </summary>
    private static string FormatFixedAxes(TST3DMatrix m, string axisOrder)
    {
        double rx, ry, rz;
        if (axisOrder == "XYZ")
        {
            // R = Rx*Ry*Rz, columns of R from TST3DMatrix vX/vY/vZ (column-major).
            // R[0,0] = vX.X, R[0,1] = vY.X, R[0,2] = vZ.X, etc.
            double r02 = m.vZ.X;
            ry = Math.Asin(Math.Clamp(r02, -1.0, 1.0));
            if (Math.Abs(r02) < 0.9999999)
            {
                rx = Math.Atan2(-m.vZ.Y, m.vZ.Z);
                rz = Math.Atan2(-m.vY.X, m.vX.X);
            }
            else
            {
                rx = Math.Atan2(m.vY.Z, m.vY.Y);
                rz = 0;
            }
        }
        else // ZYX
        {
            // R = Rz*Ry*Rx
            double r20 = m.vX.Z;
            ry = Math.Asin(Math.Clamp(-r20, -1.0, 1.0));
            if (Math.Abs(r20) < 0.9999999)
            {
                rx = Math.Atan2(m.vY.Z, m.vZ.Z);
                rz = Math.Atan2(m.vX.Y, m.vX.X);
            }
            else
            {
                rx = Math.Atan2(-m.vZ.Y, m.vY.Y);
                rz = 0;
            }
        }
        return FormatTranslationAndEuler(m, axisOrder, rx, ry, rz);
    }

    // ── Proper Euler ZXZ ──────────────────────────────────────────────────

    private static string FormatProperEulerZXZ(TST3DMatrix m)
    {
        // R = Rz(α) * Rx(β) * Rz(γ)
        double beta = Math.Acos(Math.Clamp(m.vZ.Z, -1.0, 1.0));
        double alpha, gamma;
        if (Math.Sin(beta) > 1e-9)
        {
            alpha = Math.Atan2(m.vZ.X, -m.vZ.Y);
            gamma = Math.Atan2(m.vX.Z,  m.vY.Z);
        }
        else
        {
            alpha = Math.Atan2(m.vX.Y, m.vX.X);
            gamma = 0;
        }
        return FormatTranslationAndEuler(m, "ZXZ", alpha, beta, gamma);
    }

    // ── Rotating-axes X'Y'Z' (intrinsic) ──────────────────────────────────

    private static string FormatRotatingAxesXYZ(TST3DMatrix m)
    {
        // Intrinsic XYZ is identical to extrinsic ZYX (same rotation matrix).
        // R = Rx'(rx) * Ry'(ry) * Rz'(rz)  ==  Rz(rz) * Ry(ry) * Rx(rx)
        double r20 = m.vX.Z;
        double ry  = Math.Asin(Math.Clamp(-r20, -1.0, 1.0));
        double rx, rz;
        if (Math.Abs(r20) < 0.9999999)
        {
            rx = Math.Atan2(m.vY.Z, m.vZ.Z);
            rz = Math.Atan2(m.vX.Y, m.vX.X);
        }
        else
        {
            rx = Math.Atan2(-m.vZ.Y, m.vY.Y);
            rz = 0;
        }
        return FormatTranslationAndEuler(m, "X'Y'Z'", rx, ry, rz);
    }

    // ── Quaternion ────────────────────────────────────────────────────────

    private static string FormatQuaternion(TST3DMatrix m)
    {
        // Shoemake's stable conversion from rotation matrix to quaternion.
        double m00 = m.vX.X, m01 = m.vY.X, m02 = m.vZ.X;
        double m10 = m.vX.Y, m11 = m.vY.Y, m12 = m.vZ.Y;
        double m20 = m.vX.Z, m21 = m.vY.Z, m22 = m.vZ.Z;

        double tr = m00 + m11 + m22;
        double qw, qx, qy, qz;
        if (tr > 0)
        {
            double s = Math.Sqrt(tr + 1.0) * 2;
            qw = 0.25 * s;
            qx = (m21 - m12) / s;
            qy = (m02 - m20) / s;
            qz = (m10 - m01) / s;
        }
        else if (m00 > m11 && m00 > m22)
        {
            double s = Math.Sqrt(1.0 + m00 - m11 - m22) * 2;
            qw = (m21 - m12) / s;
            qx = 0.25 * s;
            qy = (m01 + m10) / s;
            qz = (m02 + m20) / s;
        }
        else if (m11 > m22)
        {
            double s = Math.Sqrt(1.0 + m11 - m00 - m22) * 2;
            qw = (m02 - m20) / s;
            qx = (m01 + m10) / s;
            qy = 0.25 * s;
            qz = (m12 + m21) / s;
        }
        else
        {
            double s = Math.Sqrt(1.0 + m22 - m00 - m11) * 2;
            qw = (m10 - m01) / s;
            qx = (m02 + m20) / s;
            qy = (m12 + m21) / s;
            qz = 0.25 * s;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"TX = {m.vT.X.ToString("F6", Inv)}");
        sb.AppendLine($"TY = {m.vT.Y.ToString("F6", Inv)}");
        sb.AppendLine($"TZ = {m.vT.Z.ToString("F6", Inv)}");
        sb.AppendLine($"QW = {qw.ToString("F8", Inv)}");
        sb.AppendLine($"QX = {qx.ToString("F8", Inv)}");
        sb.AppendLine($"QY = {qy.ToString("F8", Inv)}");
        sb.AppendLine($"QZ = {qz.ToString("F8", Inv)}");
        return sb.ToString().TrimEnd();
    }

    // ── Shared helper ─────────────────────────────────────────────────────

    private static string FormatTranslationAndEuler(
        TST3DMatrix m, string label, double a, double b, double c)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"TX = {m.vT.X.ToString("F6", Inv)}");
        sb.AppendLine($"TY = {m.vT.Y.ToString("F6", Inv)}");
        sb.AppendLine($"TZ = {m.vT.Z.ToString("F6", Inv)}");
        sb.AppendLine($"{label}[0] = {ToDeg(a).ToString("F6", Inv)}°");
        sb.AppendLine($"{label}[1] = {ToDeg(b).ToString("F6", Inv)}°");
        sb.AppendLine($"{label}[2] = {ToDeg(c).ToString("F6", Inv)}°");
        return sb.ToString().TrimEnd();
    }

    private static double ToDeg(double rad) => rad * 180.0 / Math.PI;
}
