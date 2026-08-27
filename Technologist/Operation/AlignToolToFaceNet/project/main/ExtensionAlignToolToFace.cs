using System.Text;

using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.Machine;
using CAMAPI.MachineConfiguration;
using CAMAPI.Project;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;

using STTypes;

namespace AlignToolToFaceNet;

/// <summary>
/// Sets the tool orientation of tech operations from the normals of the planar faces selected
/// in the geometry model, which is the programmatic equivalent of picking a face in
/// "Setup tab / Tool Orientation" for every operation by hand
/// </summary>
internal class ExtensionAlignToolToFace : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Aligns the tool axis of the tech operations to the normals of the selected planar faces
    /// </summary>
    /// <param name="context">Information about current running instance</param>
    /// <param name="resultStatus">Structure to return error</param>
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            // ComWrapper keeps the lifetime of every COM object predictable for the CAM system instead of leaving it to the garbage collector
            using var applicationCom = ComWrapper.Create(context.CamApplication);
            using var projectCom = applicationCom.GetActiveProject();

            var toolAxes = ReadSelectedPlaneNormals(projectCom);
            if (toolAxes.Count == 0)
                throw new Exception("Select at least one planar face in the geometry model and run the utility again");

            // one evaluator serves the whole loop, InitMachineEvaluator re-seeds it for every operation
            using var machineCom = projectCom.Machine();
            using var evaluatorCom = machineCom.CreateEvaluator();

            using var technologistCom = projectCom.Technologist();
            using var rootOperationCom = technologistCom.RootOperation();
            var rootId = rootOperationCom.Id();

            var report = new StringBuilder();
            var axisIndex = 0;
            foreach (var operationCom in technologistCom.EnumerateOperations(TCamApiReorderingMode.rmDesigned))
            {
                if (operationCom.Id() == rootId)
                    continue;

                // the enumerator disposes the wrapper only when it advances, so a break has to do it here
                if (axisIndex >= toolAxes.Count)
                {
                    operationCom.Dispose();
                    break;
                }

                var axes = AlignOperation(operationCom, evaluatorCom, toolAxes[axisIndex]);
                report.AppendLine($"{operationCom.Name()}: {axes}");
                axisIndex++;
            }

            ShowReport(report.ToString());
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }

    /// <summary>
    /// Points the tool axis of a single operation along <paramref name="toolAxis"/> while keeping the
    /// tool tip where the operation left it, and returns the resulting axis values
    /// </summary>
    /// <remarks>
    /// The order of the calls matters: the operation state has to reach the evaluator before the
    /// solver runs, and the solution has to be applied to the evaluator before it is stored back
    /// into the operation. This covers the Vector and Point modes of the robot 6th axis. The
    /// ToolPath mode additionally needs a lead direction, which a face normal does not carry - it
    /// is solved with CalcNextPos6D, taking the lead direction from the vX column of the matrix.
    /// </remarks>
    private static string AlignOperation(
        ComWrapper<ICamApiTechOperation> operationCom,
        ComWrapper<ICamApiMachineEvaluator> evaluatorCom,
        TST3DPoint toolAxis)
    {
        using var machineConfigCom = operationCom.MachineConfiguration();

        // keeps the tool tip of the operation and replaces the tool direction only
        var position = new TST5DPoint { P = default, n = toolAxis };

        if (!SolveReachable(operationCom, machineConfigCom, evaluatorCom, ref position))
            throw new Exception($"Cannot reach the requested tool orientation in operation '{operationCom.Name()}'");

        // stores the pose the evaluator holds, indexing the axes that carry the orientation
        machineConfigCom.SetToolOrientationFromEvaluator(evaluatorCom);

        return $"{DescribeDefinedAxes(machineConfigCom)} (flips: {DescribeFlips(machineConfigCom)})";
    }

    /// <summary>
    /// Solves the requested tool direction into a pose the machine can actually hold, walking the
    /// inverse-kinematics branches until one lands inside the axis limits
    /// </summary>
    /// <remarks>
    /// A machine with rotary axes - a robot above all - reaches the same tool direction with several
    /// joint solutions, selected by the flips of the operation. The branch the current flips happen
    /// to select may be unreachable, and CalcNextPos5D still returns true for it: reaching the point
    /// and staying inside the limits are two different questions. NextPosOutOfLimits answers the
    /// second one, and it is the same check that paints an axis value red in the machine control
    /// panel. On a robot the flip set also covers the external axes of a positioner, so letting the
    /// sweep turn one on is often what makes a pose reachable at all.
    /// </remarks>
    private static bool SolveReachable(
        ComWrapper<ICamApiTechOperation> operationCom,
        ComWrapper<ICamApiMachineConfiguration> machineConfigCom,
        ComWrapper<ICamApiMachineEvaluator> evaluatorCom,
        ref TST5DPoint position)
    {
        var flipsCount = machineConfigCom.FlipsCount();
        for (var mask = 0; mask < (1 << flipsCount); mask++)
        {
            for (var flip = 0; flip < flipsCount; flip++)
                machineConfigCom.SetFlipEnabled(flip, (mask & (1 << flip)) != 0);

            // re-seeds the evaluator so the flips just written reach the solver
            operationCom.InitMachineEvaluator(evaluatorCom);
            position.P = evaluatorCom.GetAbsoluteMatrix().vT;

            if (!evaluatorCom.CalcNextPos5D(position, false, false, true))
                continue;

            if (evaluatorCom.NextPosOutOfLimits())
                continue;

            // moves the solution into the evaluator state, without it the previous pose is stored
            evaluatorCom.SetNextPos(false);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Reads the unit normal of every planar face selected in the geometry model
    /// </summary>
    private static List<TST3DPoint> ReadSelectedPlaneNormals(ComWrapper<ICamApiProject> projectCom)
    {
        using var geomModelCom = projectCom.CAMAPIGeomModel();
        using var facesCom = geomModelCom.GetFaceListOfSelected();
        var facesCount = facesCom.Invoke(faces => faces.Count);

        var normals = new List<TST3DPoint>();
        for (var i = 0; i < facesCount; i++)
        {
            using var faceCom = facesCom.InvokeAndWrap(faces => faces.Face[i]);

            // a non-planar face carries no single approach direction to take the tool axis from
            if (faceCom.GetPlane(out _, out var normal))
                normals.Add(normal);
        }
        return normals;
    }

    /// <summary>
    /// Lists the axes the solver has just written into the operation
    /// </summary>
    private static string DescribeDefinedAxes(ComWrapper<ICamApiMachineConfiguration> machineConfigCom)
    {
        var values = new List<string>();
        var axesCount = machineConfigCom.AxesCount();
        for (var i = 0; i < axesCount; i++)
        {
            if (!machineConfigCom.AxisAvailable(i) || !machineConfigCom.AxisDefined(i))
                continue;

            values.Add($"{machineConfigCom.AxisId(i)}={machineConfigCom.AxisValue(i):F3}");
        }
        return values.Count == 0 ? "no axes defined" : string.Join(" ", values);
    }

    /// <summary>
    /// Lists the inverse-kinematics branches of the operation and their state
    /// </summary>
    private static string DescribeFlips(ComWrapper<ICamApiMachineConfiguration> machineConfigCom)
    {
        var states = new List<string>();
        var flipsCount = machineConfigCom.FlipsCount();
        for (var i = 0; i < flipsCount; i++)
            states.Add($"{machineConfigCom.FlipId(i)}={(machineConfigCom.FlipEnabled(i) ? "on" : "off")}");

        return states.Count == 0 ? "none" : string.Join(" ", states);
    }

    /// <summary>
    /// Shows what has been written into every processed operation
    /// </summary>
    private static void ShowReport(string text)
    {
        using var dialogsCom = UIDialogs.CreateHelper();
        if (dialogsCom.IsNull)
            return;

        dialogsCom.Invoke(dialogs => dialogs.MessageBox(text, TMessageDialogType.mdtInformation,
            (ushort)TUIButtonTypeFlags.btfOk, TUIButtonType.btOk, "Align tool to face"));
    }
}
