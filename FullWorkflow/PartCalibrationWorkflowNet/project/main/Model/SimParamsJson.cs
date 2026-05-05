namespace PartCalibrationWorkflowNet.Model;

/// <summary>6-DOF simulation parameters stored in measured.json for traceability.</summary>
public class SimParamsJson
{
    /// <summary>Translation along X (mm).</summary>
    public double TX { get; set; }

    /// <summary>Translation along Y (mm).</summary>
    public double TY { get; set; }

    /// <summary>Translation along Z (mm).</summary>
    public double TZ { get; set; }

    /// <summary>Rotation around X axis (degrees).</summary>
    public double RX { get; set; }

    /// <summary>Rotation around Y axis (degrees).</summary>
    public double RY { get; set; }

    /// <summary>Rotation around Z axis (degrees).</summary>
    public double RZ { get; set; }
}
