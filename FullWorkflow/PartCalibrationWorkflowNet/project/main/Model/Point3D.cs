namespace PartCalibrationWorkflowNet.Model;

/// <summary>
/// Plain serializable 3D point with optional surface normal. Used by the
/// imported-points parsers and the deviation-calculation pipeline.
/// </summary>
public sealed class Point3D
{
    public string Name { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double Nx { get; set; }
    public double Ny { get; set; }
    public double Nz { get; set; }
}
