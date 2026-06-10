using STTypes;

namespace PartCalibrationWorkflowNet.Model;

/// <summary>
/// Result of a Kabsch fit produced on Tab 4 and consumed on Tab 5.
/// </summary>
public sealed class CalibrationResult
{
    public required TST3DMatrix Matrix { get; init; }
    public required double MaxResidual { get; init; }
    public required int PointCount { get; init; }
}
