using CAMAPI.DotnetHelper;
using CAMAPI.Project;
using STTypes;

namespace PartCalibrationWorkflowNet.Service;

/// <summary>
/// Applies a calibration transformation to the active project:
/// sets the workpiece offset of Setup 2 to the calibrated matrix.
/// </summary>
internal sealed class CalibrationApplyService
{
    /// <summary>
    /// Sets the workpiece offset of Setup 2 to <paramref name="matrix"/>.
    /// </summary>
    public void Apply(
        ComWrapper<ICamApiProject> projCom,
        TST3DMatrix matrix)
    {
        using var techCom = projCom.Technologist();
        using var partAndStageList = techCom.PartAndStageList();
        using var partStage = partAndStageList.GetPartStage(0, 1);
        using var wpSetupCom = partStage.WorkpieceSetup();
        wpSetupCom.SetOffset(matrix);
    }
}
