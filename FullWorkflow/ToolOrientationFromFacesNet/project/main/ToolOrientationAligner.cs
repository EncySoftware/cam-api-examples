using CAMAPI.DotnetHelper;
using CAMAPI.Machine;
using CAMAPI.MachineConfiguration;
using CAMAPI.Project;
using CAMAPI.TechOperation;
using CAMAPI.Technologist;

using STTypes;

namespace ToolOrientationFromFacesNet;

/// <summary>
/// What one operation ended up with after the tool axis was pointed along its face normal
/// </summary>
/// <param name="Name">Name of the operation</param>
/// <param name="Normal">Face normal the tool axis was pointed along</param>
/// <param name="Axes">Axis values the machine solver produced</param>
public record AlignmentInfo(string Name, string Normal, string Axes);

/// <summary>
/// Tool axis an operation actually ends up with, measured back out of its stored axis values
/// </summary>
/// <param name="ToolAxis">Direction of the tool, in the coordinate system the face normals are given in</param>
/// <param name="AngleToNormal">Angle between that direction and the face normal the operation was given, in degrees</param>
public record ToolAxisInfo(string ToolAxis, double AngleToNormal);

/// <summary>
/// Points the tool axis of every operation along the normal of its own approach plane, which is the
/// programmatic equivalent of picking a face in "Setup tab / Tool Orientation" for every operation
/// </summary>
public static class ToolOrientationAligner
{
    /// <summary>
    /// Give every operation the tool orientation of its own face normal
    /// </summary>
    /// <remarks>
    /// When there are fewer normals than operations the normals are reused, so that the loop keeps
    /// working whatever the model and the operation count are.
    /// </remarks>
    public static List<AlignmentInfo> AlignOperations(ComWrapper<ICamApiProject> projectCom,
        ComWrapper<ICamApiTechnologist> technologistCom,
        List<TST3DPoint> toolAxes)
    {
        // one evaluator serves the whole loop, InitMachineEvaluator re-seeds it for every operation
        using var machineCom = projectCom.Machine();
        using var evaluatorCom = machineCom.CreateEvaluator();

        using var rootOperationCom = technologistCom.RootOperation();
        var rootId = rootOperationCom.Id();

        var alignments = new List<AlignmentInfo>();
        foreach (var operationCom in technologistCom.EnumerateOperations(TCamApiReorderingMode.rmDesigned))
        {
            if (operationCom.Id() == rootId)
                continue;

            var toolAxis = toolAxes[alignments.Count % toolAxes.Count];
            alignments.Add(new AlignmentInfo(
                operationCom.Name(),
                Describe(toolAxis),
                AlignOperation(operationCom, evaluatorCom, toolAxis)));
        }
        return alignments;
    }

    /// <summary>
    /// Read back the tool axis every operation ends up with and compare it with the face normal it
    /// was given, which is what tells the orientation apart from merely non-zero axis values
    /// </summary>
    /// <remarks>
    /// InitMachineEvaluator applies the axis values stored in the operation to the evaluator, so the
    /// vZ column of the resulting matrix is the direction the tool really points at - the machine
    /// solver is asked to state its own result instead of the caller assuming the request took.
    /// </remarks>
    public static List<ToolAxisInfo> MeasureToolAxes(ComWrapper<ICamApiProject> projectCom,
        ComWrapper<ICamApiTechnologist> technologistCom,
        List<TST3DPoint> toolAxes)
    {
        using var machineCom = projectCom.Machine();
        using var evaluatorCom = machineCom.CreateEvaluator();

        using var rootOperationCom = technologistCom.RootOperation();
        var rootId = rootOperationCom.Id();

        var measurements = new List<ToolAxisInfo>();
        foreach (var operationCom in technologistCom.EnumerateOperations(TCamApiReorderingMode.rmDesigned))
        {
            if (operationCom.Id() == rootId)
                continue;

            var requested = toolAxes[measurements.Count % toolAxes.Count];

            operationCom.InitMachineEvaluator(evaluatorCom);
            var toolAxis = evaluatorCom.GetAbsoluteMatrix().vZ;

            measurements.Add(new ToolAxisInfo(Describe(toolAxis), AngleBetween(toolAxis, requested)));
        }
        return measurements;
    }

    /// <summary>
    /// Angle between two directions, in degrees
    /// </summary>
    private static double AngleBetween(TST3DPoint left, TST3DPoint right)
    {
        var leftLength = Math.Sqrt(left.X * left.X + left.Y * left.Y + left.Z * left.Z);
        var rightLength = Math.Sqrt(right.X * right.X + right.Y * right.Y + right.Z * right.Z);
        if (leftLength == 0 || rightLength == 0)
            return double.NaN;

        var cosine = (left.X * right.X + left.Y * right.Y + left.Z * right.Z) / (leftLength * rightLength);
        return Math.Acos(Math.Clamp(cosine, -1.0, 1.0)) * 180.0 / Math.PI;
    }

    /// <summary>
    /// Throw away the toolpath of every operation and calculate it again
    /// </summary>
    /// <remarks>
    /// Storing the orientation is a deferred write: it stores the axes but leaves the toolpath alone
    /// and does not drop the "calculated" state of the operation, so until the toolpath is calculated
    /// again nothing changes on screen and the simulation replays the very same frames.
    /// </remarks>
    public static void RecalculateToolpath(ComWrapper<ICamApiTechnologist> technologistCom)
    {
        technologistCom.ResetAllOperationsToolpath();
        technologistCom.CalculateAllOperationsToolpath(true);
    }

    /// <summary>
    /// Point the tool axis of a single operation along <paramref name="toolAxis"/> while keeping the
    /// tool tip where the operation left it, and return the resulting axis values
    /// </summary>
    /// <remarks>
    /// The order of the calls matters and none of them can be dropped: the operation state has to
    /// reach the evaluator before the solver runs, and the solution has to be applied to the
    /// evaluator before it is stored back into the operation. This covers the Vector and Point modes
    /// of the robot 6th axis. The ToolPath mode additionally needs a lead direction, which a face
    /// normal does not carry - it is solved with CalcNextPos6D, taking the lead direction from the
    /// vX column of the matrix.
    /// </remarks>
    private static string AlignOperation(ComWrapper<ICamApiTechOperation> operationCom,
        ComWrapper<ICamApiMachineEvaluator> evaluatorCom,
        TST3DPoint toolAxis)
    {
        using var machineConfigCom = operationCom.MachineConfiguration();

        // seeds the evaluator with the state of this operation - flips, robot mode, setup coordinate system, external axes
        operationCom.InitMachineEvaluator(evaluatorCom);

        // keeps the tool tip of the operation and replaces the tool direction only
        var position = new TST5DPoint
        {
            P = evaluatorCom.GetAbsoluteMatrix().vT,
            n = toolAxis
        };

        if (!SolveReachable(operationCom, machineConfigCom, evaluatorCom, position))
            throw new Exception($"Cannot reach the requested tool orientation in operation '{operationCom.Name()}'");

        // stores the pose the evaluator holds, indexing the axes that carry the orientation
        machineConfigCom.SetToolOrientationFromEvaluator(evaluatorCom);

        return $"{DescribeDefinedAxes(machineConfigCom)} (flips: {DescribeFlips(machineConfigCom)})";
    }

    /// <summary>
    /// Solve the requested tool direction into a pose the machine can actually hold, walking the
    /// inverse-kinematics branches until one lands inside the axis limits
    /// </summary>
    /// <remarks>
    /// A machine with rotary axes - a robot above all - reaches the same tool direction with several
    /// joint solutions, and the solver returns whichever branch the current flips select. That branch
    /// may be unreachable, and CalcNextPos5D still returns true for it: reaching the point and staying
    /// inside the limits are two different questions. NextPosOutOfLimits answers the second one, and
    /// it is the same check that paints an axis value red in the machine control panel.
    /// The API has no flip iterator of its own, so the combinations are swept here.
    /// </remarks>
    private static bool SolveReachable(ComWrapper<ICamApiTechOperation> operationCom,
        ComWrapper<ICamApiMachineConfiguration> machineConfigCom,
        ComWrapper<ICamApiMachineEvaluator> evaluatorCom,
        TST5DPoint position)
    {
        var flipsCount = machineConfigCom.FlipsCount();
        var combinations = 1 << flipsCount;

        for (var mask = 0; mask < combinations; mask++)
        {
            for (var flip = 0; flip < flipsCount; flip++)
                machineConfigCom.SetFlipEnabled(flip, (mask & (1 << flip)) != 0);

            // re-seeds the evaluator so the flips just written reach the solver
            operationCom.InitMachineEvaluator(evaluatorCom);
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
    /// List the axes the solver has just written into the operation
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
    /// List the inverse-kinematics branches of the operation and their state. A robot reaches the
    /// same tool direction with several joint solutions, so axes that disagree with the UI for the
    /// very same direction mean a different branch, not a different mode
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
    /// Format a direction for the report
    /// </summary>
    private static string Describe(TST3DPoint normal)
        => $"({normal.X:F3}, {normal.Y:F3}, {normal.Z:F3})";
}
