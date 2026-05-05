namespace PartCalibrationWorkflowNet.Model;

/// <summary>6-DOF simulation parameters entered by user.</summary>
internal record SimulatorParams(
    double TX, double TY, double TZ,
    double RX, double RY, double RZ);
