namespace PartCalibrationWorkflowNet.Model;

/// <summary>
/// Display format for the calibration result on Tab 4 / Tab 5.
/// Mirrors the formats supported by STLibraries/STLib/EulerAngles.pas and
/// DotNet/VecMatrLib/EulerConverter.cs.
/// </summary>
public enum RotationFormat
{
    /// <summary>4x4 matrix (row-major, 16 numbers)</summary>
    Matrix4x4,

    /// <summary>Translation + Euler XYZ (fixed-axis, degrees)</summary>
    EulerXYZ,

    /// <summary>Translation + Euler ZYX (fixed-axis, degrees)</summary>
    EulerZYX,

    /// <summary>Translation + Euler ZXZ (proper, degrees)</summary>
    EulerZXZ,

    /// <summary>Translation + Euler X'Y'Z' (rotating-axes, degrees)</summary>
    EulerXpYpZp,

    /// <summary>Translation + Quaternion (qw, qx, qy, qz)</summary>
    Quaternion,
}
