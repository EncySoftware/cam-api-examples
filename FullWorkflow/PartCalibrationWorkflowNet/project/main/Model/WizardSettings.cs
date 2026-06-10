namespace PartCalibrationWorkflowNet.Model;

/// <summary>
/// Persistent settings for the wizard. Saved to a JSON file beside the
/// extension DLL and rehydrated when the window is reopened.
/// </summary>
public sealed class WizardSettings
{
    // Tab 1
    // Empty string means "root of the Model page" — safer default than a
    // hard-coded folder name that may not exist in the current project.
    public string PointsParentFolder { get; set; } = "";
    public string PointsFolderName { get; set; } = "ProbePoints";
    public int PointsCount { get; set; } = 8;

    // Tab 2
    public string CycleType { get; set; } = "SurfaceCycle";

    // Tab 3
    public string MeasuredParentFolder { get; set; } = "";
    public string MeasuredFolderName { get; set; } = "MeasuredPoints";
    public string MeasuredParser { get; set; } = "Plain text (X;Y;Z)";
    public string MeasuredFilePath { get; set; } = "";

    // Tab 4
    public string DeviationPointsFolder { get; set; } = "Model/MeasuredPoints";
    public string DeviationNominalFolder { get; set; } = "";
    public string OutputFormat { get; set; } = "EulerZYX";

    // Tab 5
    public string ApplyMode { get; set; } = "CreateLCS";
    public string LcsName { get; set; } = "CalibratedLCS";
    public string Target3DModelFolder { get; set; } = "";
}
