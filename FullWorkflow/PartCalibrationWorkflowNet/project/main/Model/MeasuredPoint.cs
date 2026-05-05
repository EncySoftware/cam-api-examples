namespace PartCalibrationWorkflowNet.Model;

/// <summary>Measured probe point returned by the machine.</summary>
public class MeasuredPoint
{
    /// <summary>Component index (1-based) as written in the NC report action.</summary>
    public int    ComponentNumber { get; set; }

    /// <summary>Feature index (1-based) as written in the NC report action.</summary>
    public int    FeatureNumber   { get; set; }

    /// <summary>Measured X coordinate in world space (mm).</summary>
    public double X               { get; set; }

    /// <summary>Measured Y coordinate in world space (mm).</summary>
    public double Y               { get; set; }

    /// <summary>Measured Z coordinate in world space (mm).</summary>
    public double Z               { get; set; }
}
