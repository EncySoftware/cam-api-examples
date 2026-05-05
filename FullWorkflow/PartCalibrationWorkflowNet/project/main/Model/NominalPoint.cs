namespace PartCalibrationWorkflowNet.Model;

/// <summary>Nominal probe point in model space with surface normal.</summary>
public class NominalPoint
{
    /// <summary>X coordinate in model space (mm).</summary>
    public double X  { get; set; }

    /// <summary>Y coordinate in model space (mm).</summary>
    public double Y  { get; set; }

    /// <summary>Z coordinate in model space (mm).</summary>
    public double Z  { get; set; }

    /// <summary>X component of the surface normal at this point.</summary>
    public double NX { get; set; }

    /// <summary>Y component of the surface normal at this point.</summary>
    public double NY { get; set; }

    /// <summary>Z component of the surface normal at this point.</summary>
    public double NZ { get; set; }
}
