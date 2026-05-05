namespace PartCalibrationWorkflowNet.Model;

/// <summary>Root object for measured.json.</summary>
public class MeasuredData
{
    /// <summary>Simulation parameters used to generate this measurement.</summary>
    public SimParamsJson?      SimulationParams { get; set; }

    /// <summary>Measured probe points returned by the machine.</summary>
    public List<MeasuredPoint> Points           { get; set; } = new();
}
