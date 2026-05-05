using System.IO;
using System.Text.Json;
using PartCalibrationWorkflowNet.Model;

namespace PartCalibrationWorkflowNet.Service;

/// <summary>
/// Simulates real machine measurement results by applying a 6-DOF rigid-body transform
/// (rotation Rz*Ry*Rx followed by translation) to nominal probe points.
/// Writes measured.json to the plugin directory.
/// No COM objects are involved — pure data transformation.
/// </summary>
internal sealed class MachineSimulationService
{
    private readonly string _pluginDir;

    public MachineSimulationService(string pluginDir)
    {
        _pluginDir = pluginDir;
    }

    /// <summary>
    /// Loads nominal.json, applies the given 6-DOF offset, writes measured.json.
    /// Returns the full path to the written measured.json.
    /// </summary>
    public string Simulate(SimulatorParams p)
    {
        var nominalPath = Path.Combine(_pluginDir, "nominal.json");
        if (!File.Exists(nominalPath))
            throw new FileNotFoundException("nominal.json not found — run 'Create Project' first.", nominalPath);

        var nominal = JsonSerializer.Deserialize<NominalData>(File.ReadAllText(nominalPath))
            ?? throw new Exception("Failed to parse nominal.json");

        var measuredPoints = ApplyTransform(nominal.Points, p);

        var measuredPath = Path.Combine(_pluginDir, "measured.json");
        File.WriteAllText(measuredPath, JsonSerializer.Serialize(
            new MeasuredData
            {
                SimulationParams = new SimParamsJson
                {
                    TX = p.TX, TY = p.TY, TZ = p.TZ,
                    RX = p.RX, RY = p.RY, RZ = p.RZ
                },
                Points = measuredPoints
            },
            new JsonSerializerOptions { WriteIndented = true }));

        return measuredPath;
    }

    private static List<MeasuredPoint> ApplyTransform(List<NominalPoint> nominals, SimulatorParams p)
    {
        double rx = p.RX * Math.PI / 180.0;
        double ry = p.RY * Math.PI / 180.0;
        double rz = p.RZ * Math.PI / 180.0;

        double cx = Math.Cos(rx), sx = Math.Sin(rx);
        double cy = Math.Cos(ry), sy = Math.Sin(ry);
        double cz = Math.Cos(rz), sz = Math.Sin(rz);

        // R = Rz * Ry * Rx  (column-major notation)
        double r00 = cz * cy,       r01 = cz * sy * sx - sz * cx, r02 = cz * sy * cx + sz * sx;
        double r10 = sz * cy,       r11 = sz * sy * sx + cz * cx, r12 = sz * sy * cx - cz * sx;
        double r20 = -sy,           r21 = cy * sx,                r22 = cy * cx;

        var result = new List<MeasuredPoint>(nominals.Count);
        for (int i = 0; i < nominals.Count; i++)
        {
            var n = nominals[i];
            result.Add(new MeasuredPoint
            {
                ComponentNumber = i + 1,
                X = r00 * n.X + r01 * n.Y + r02 * n.Z + p.TX,
                Y = r10 * n.X + r11 * n.Y + r12 * n.Z + p.TY,
                Z = r20 * n.X + r21 * n.Y + r22 * n.Z + p.TZ
            });
        }
        return result;
    }
}
