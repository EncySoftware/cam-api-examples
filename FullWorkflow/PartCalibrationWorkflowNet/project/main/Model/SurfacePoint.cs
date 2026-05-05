using STTypes;

namespace PartCalibrationWorkflowNet.Model;

/// <summary>One surface sample: model-space and world-space position + normal.</summary>
internal record SurfacePoint(
    TST3DPoint ModelPosition,
    TST3DPoint ModelNormal,
    TST3DPoint WorldPosition,
    TST3DPoint WorldNormal,
    string FaceFullName = "");
